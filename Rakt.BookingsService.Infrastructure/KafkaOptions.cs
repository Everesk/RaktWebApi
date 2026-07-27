namespace Rakt.BookingsService.Infrastructure;

/// <summary>Настройки подключения к Kafka.</summary>
public sealed class KafkaOptions
{
    /// <summary>Имя секции настроек Kafka.</summary>
    public const string SectionName = "Kafka";

    /// <summary>Адреса брокеров Kafka.</summary>
    public string BootstrapServers { get; init; } = string.Empty;

    /// <summary>Имя группы потребителей результатов резервирования.</summary>
    public string ConsumerGroup { get; init; } = string.Empty;
}
