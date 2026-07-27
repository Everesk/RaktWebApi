using System.Diagnostics;
using Npgsql;

namespace Rakt.E2ETests;

/// <summary>
/// Запускает локальные микросервисы для сквозных тестов поверх Docker-инфраструктуры.
/// </summary>
public sealed class MicroservicesE2eFixture : IAsyncLifetime
{
    private const string UsersConnectionString = "Host=localhost;Port=5434;Database=rakt_users;Username=postgres;Password=postgres";
    private const string EventsConnectionString = "Host=localhost;Port=5435;Database=rakt_events;Username=postgres;Password=postgres";
    private const string BookingsConnectionString = "Host=localhost;Port=5436;Database=rakt_bookings;Username=postgres;Password=postgres";
    private readonly List<Process> _processes = [];

    /// <summary>
    /// Адрес API пользователей.
    /// </summary>
    public Uri UsersBaseAddress { get; } = new("http://localhost:5008/");

    /// <summary>
    /// Адрес API событий.
    /// </summary>
    public Uri EventsBaseAddress { get; } = new("http://localhost:5009/");

    /// <summary>
    /// Адрес API броней.
    /// </summary>
    public Uri BookingsBaseAddress { get; } = new("http://localhost:5010/");

    /// <summary>
    /// Очищает базы Docker-инфраструктуры и запускает три HTTP-сервиса.
    /// </summary>
    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync(UsersConnectionString);
        await ResetDatabaseAsync(EventsConnectionString);
        await ResetDatabaseAsync(BookingsConnectionString);

        StartService("Rakt.UsersService.Presentation/Rakt.UsersService.Presentation.csproj");
        StartService("Rakt.EventsService.Presentation/Rakt.EventsService.Presentation.csproj");
        StartService("Rakt.BookingsService.Presentation/Rakt.BookingsService.Presentation.csproj");

        await WaitForHealthAsync(UsersBaseAddress);
        await WaitForHealthAsync(EventsBaseAddress);
        await WaitForHealthAsync(BookingsBaseAddress);
    }

    /// <summary>
    /// Останавливает локально запущенные процессы сервисов.
    /// </summary>
    public async Task DisposeAsync()
    {
        foreach (var process in _processes)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync();
            process.Dispose();
        }
    }

    /// <summary>
    /// Запускает сервис с HTTP-профилем launchSettings без повторной сборки.
    /// </summary>
    private void StartService(string projectPath)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --no-build --project \"{projectPath}\" --launch-profile http",
            WorkingDirectory = GetSolutionDirectory(),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не удалось запустить сервис {projectPath}.");

        _processes.Add(process);
    }

    /// <summary>
    /// Ожидает готовности HTTP-проверки состояния сервиса.
    /// </summary>
    private static async Task WaitForHealthAsync(Uri baseAddress)
    {
        using var client = new HttpClient
        {
            BaseAddress = baseAddress
        };
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(45));

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync("health", cancellationTokenSource.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationTokenSource.Token);
        }

        throw new TimeoutException($"Сервис {baseAddress} не ответил на health-проверку.");
    }

    /// <summary>
    /// Очищает публичную схему базы перед запуском миграций сервисом.
    /// </summary>
    private static async Task ResetDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;";

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Находит корень решения независимо от папки запуска тестов.
    /// </summary>
    private static string GetSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RaktWebApi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Не найден корень решения RaktWebApi.sln.");
    }
}
