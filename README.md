# Rakt — микросервисная система событий и бронирований

Учебный проект, спринты 1–10. Система на .NET 9 разделена на независимые сервисы пользователей, событий и бронирований. Каждый сервис использует чистую архитектуру и собственную PostgreSQL-базу; обмен между сервисами выполняется только через Kafka.

## Возможности

- регистрация пользователей, вход и выпуск JWT;
- CRUD событий с учётом доступных мест;
- асинхронное создание, подтверждение, отклонение и отмена броней;
- защита от овербукинга при конкурентных запросах;
- Swagger, централизованная обработка ошибок и Serilog во всех HTTP-сервисах;
- unit-, integration- и E2E-тесты.

## Стек

- .NET 9, ASP.NET Core Web API;
- EF Core и PostgreSQL 16;
- Confluent.Kafka и Apache Kafka;
- Redis и StackExchange.Redis;
- JWT Bearer Authentication;
- Serilog;
- xUnit и Testcontainers.

## Структура решения

Каждый сервис состоит из слоёв `Domain`, `Application`, `Infrastructure`, `Presentation`.

| Сервис | Назначение | HTTPS-порт | База данных / порт |
| --- | --- | ---: | --- |
| `Rakt.UsersService.*` | пользователи, пароль, JWT | 7131 | `rakt_users` / 5434 |
| `Rakt.EventsService.*` | события и свободные места | 7132 | `rakt_events` / 5435 |
| `Rakt.BookingsService.*` | создание, статусы и отмена броней | 7133 | `rakt_bookings` / 5436 |
| `Rakt.Contracts` | публичные Kafka-контракты и имена топиков | — | — |

Тестовые проекты разделены по уровню проверки:

- `Rakt.*Service.UnitTests` — изолированные unit-тесты;
- `Rakt.*Service.IntegrationTests` — EF Core и Kafka через Testcontainers;
- `Rakt.E2ETests` — полный цикл между тремя локально запущенными сервисами и Docker-инфраструктурой.

## Границы данных

У сервисов нет общей схемы и нет навигационных свойств между сервисами.

- Users хранит только пользователей.
- Events хранит события и состояние обработки резервирований мест.
- Bookings хранит только брони, `UserId` и `EventId` как обычные идентификаторы.

Бронирования не обращаются к БД Events или Users напрямую.

## Взаимодействие микросервисов

Users выдаёт JWT. Events и Bookings проверяют этот токен по общим значениям `Jwt:Secret`, `Jwt:Issuer` и `Jwt:Audience` в своих конфигурациях.

Обмен между Events и Bookings построен на Kafka. При создании брони Bookings сохраняет её в статусе `Pending` и публикует `BookingRequested` в топик `booking-requested`. Events получает запрос, резервирует место либо фиксирует отказ, после чего публикует `SeatsReserved` или `SeatsReservationRejected`. Bookings получает ответ и переводит бронь в итоговый статус `Confirmed` или `Rejected`.

При отмене Bookings публикует `BookingCancelled` в топик `booking-cancelled`. Events получает сообщение и возвращает место, только если оно было ранее занято для этой брони.

### Kafka-топики и контракты

| Топик | Сообщение | Издатель | Подписчик |
| --- | --- | --- | --- |
| `booking-requested` | `BookingRequested` | Bookings | Events |
| `seats-reserved` | `SeatsReserved` | Events | Bookings |
| `seats-reservation-rejected` | `SeatsReservationRejected` | Events | Bookings |
| `booking-cancelled` | `BookingCancelled` | Bookings | Events |

Ключ каждого Kafka-сообщения — `EventId`. Поэтому команды одного события попадают в один partition и обрабатываются в порядке этого ключа.

### Идемпотентность и отмена

Events сохраняет состояние обработки по `BookingId` в таблице `booking_seat_reservations`.

- повторный `booking-requested` не списывает место второй раз;
- повторный `booking-cancelled` не возвращает место второй раз;
- если отмена пришла раньше запроса на резервирование, сохраняется маркер отмены и поздний запрос не занимает место;
- если обработчик завершился после сохранения БД, но до Kafka commit, сообщение будет прочитано повторно безопасно.

Это также исключает овербукинг: при ограниченном количестве мест часть броней получает `Confirmed`, остальные — `Rejected`.

## Локальный запуск

### 1. Поднять инфраструктуру

Docker Compose запускает Redis, Kafka, ZooKeeper и три PostgreSQL-базы; сами сервисы запускаются через `dotnet run`.

```bash
docker compose up -d
```

Проверить контейнеры:

```bash
docker compose ps
```

Остановить инфраструктуру:

```bash
docker compose down
```

Для удаления данных томов:

```bash
docker compose down -v
```

### 2. Запустить сервисы

В отдельных терминалах из корня решения:

```bash
dotnet run --project Rakt.UsersService.Presentation/Rakt.UsersService.Presentation.csproj --launch-profile https
dotnet run --project Rakt.EventsService.Presentation/Rakt.EventsService.Presentation.csproj --launch-profile https
dotnet run --project Rakt.BookingsService.Presentation/Rakt.BookingsService.Presentation.csproj --launch-profile https
```

При старте каждый сервис применяет свои EF Core-миграции. Events также создаёт необходимые Kafka-топики до запуска consumers.

Swagger доступен в Development-режиме:

- `https://localhost:7131/swagger` — Users;
- `https://localhost:7132/swagger` — Events;
- `https://localhost:7133/swagger` — Bookings.

Health endpoints: `/health` на каждом сервисе.

## JWT и роли

JWT выдаёт только Users. Общие JWT-настройки должны совпадать в трёх `appsettings.json`.

| Роль | Доступ |
| --- | --- |
| `User` | вход, создание и отмена собственной брони |
| `Admin` | права User и создание, обновление, удаление событий; отмена любой брони |

Передавайте токен в заголовке:

```http
Authorization: Bearer <token>
```

## HTTP API

### Users — `https://localhost:7131`

- `POST /auth/register` — регистрация, `204 No Content`;
- `POST /auth/login` — вход и получение JWT.

### Events — `https://localhost:7132`

- `GET /events` — список с фильтрацией и пагинацией;
- `GET /events/{id}` — одно событие;
- `GET /events/top` — десять событий с наибольшей долей забронированных мест;
- `POST /events` — создать событие, только `Admin`;
- `PUT /events/{id}` — изменить событие, только `Admin`;
- `DELETE /events/{id}` — удалить событие, только `Admin`.

### Bookings — `https://localhost:7133`

Все endpoints требуют JWT.

- `POST /bookings` — создать Pending-бронь:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000"
}
```

- `GET /bookings/{id}` — получить текущий статус;
- `GET /bookings/by-event/{eventId}` — список броней события;
- `DELETE /bookings/{id}` — отменить бронь.

`POST /bookings` возвращает `202 Accepted`. Итоговый статус появляется асинхронно после ответа Events: `Confirmed`, `Rejected` либо `Cancelled`.

## Кеширование Events

Сервис Events использует Redis через интерфейс `ICache` из слоя Application. Реализация Redis находится в Infrastructure, поэтому провайдер кеша можно заменить без изменения прикладных сценариев. Для чтения применяется Cache-Aside: сервис сначала проверяет Redis, а при промахе читает PostgreSQL и прогревает кеш. Если Redis недоступен или возвращает таймаут, ошибка записывается в лог, но не меняет HTTP-ответ: чтение продолжится из PostgreSQL, а запись или удаление кеша будет пропущено.

- `GET /events/{id}` использует ключ `event:{id}`. Это частый запрос к отдельному изменяемому объекту, поэтому выбрана стратегия обновления при записи: после успешного создания или обновления события его актуальные данные записываются в кеш; после удаления ключ удаляется.
- Kafka-сообщение `booking-requested` уменьшает остаток мест, а `booking-cancelled` его возвращает. Оба обработчика сначала сохраняют изменение в PostgreSQL, затем обновляют `event:{id}`. Поэтому данные события остаются актуальны и при асинхронных изменениях, а сбой между БД и Redis не делает БД устаревшей.
- `GET /events/top` использует ключ `events:top10` и содержит рейтинг по доле забронированных мест: `(total_seats - available_seats) / total_seats`. Его кеш не инвалидируется при изменении отдельных событий или бронированиях: небольшое устаревание рейтинга допустимо, а дополнительные операции с Redis на каждое бронирование не нужны.
- Строка подключения задаётся параметром `Redis:ConnectionString` в `Rakt.EventsService.Presentation/appsettings.json`; внутри Docker-сети используется `redis:6379`. Её можно переопределить переменной окружения `Redis__ConnectionString`.
- Время жизни отдельного события и рейтинга задаётся параметрами `Cache:EventTimeToLiveMinutes` и `Cache:TopEventsTimeToLiveMinutes`; оба по умолчанию равны 5 минутам. Это ограничивает период возможного устаревания при недоступности обновления кеша, но заметно снижает количество обращений к БД для популярных запросов.

Во всех сценариях сначала сохраняется база данных, затем изменяется Redis. Поэтому сбой между этими операциями не приводит к устаревшим данным в БД: следующий запрос прочитает актуальные данные либо после истечения TTL, либо при следующей записи в кеш.

## Миграции EF Core

Миграции применяются автоматически при старте Presentation-проектов. Для создания новой миграции используйте Infrastructure-проект соответствующего сервиса:

```bash
dotnet ef migrations add <MigrationName> \
  --project Rakt.EventsService.Infrastructure/Rakt.EventsService.Infrastructure.csproj \
  --startup-project Rakt.EventsService.Presentation/Rakt.EventsService.Presentation.csproj \
  --context EventsDbContext \
  --output-dir Migrations
```

Замените Events-пути и контекст на Users или Bookings при необходимости.

## Тестирование

Unit-тесты:

```bash
dotnet test RaktWebApi.sln --filter "Category=Unit"
```

Integration-тесты (Docker daemon должен быть запущен):

```bash
dotnet test RaktWebApi.sln --filter "Category=Integration"
```

E2E-тесты требуют поднятой `docker compose` инфраструктуры:

```bash
dotnet test Rakt.E2ETests/Rakt.E2ETests.csproj --filter "Category=E2E"
```

E2E-набор проверяет обычный цикл брони, конкурентные запросы с ограниченным числом мест и немедленную отмену.
