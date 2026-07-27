using System.ComponentModel.DataAnnotations;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Настройки подключения сервиса событий к Kafka.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Имя секции настроек Kafka.
    /// </summary>
    public const string SectionName = "Kafka";

    /// <summary>
    /// Адреса Kafka-брокеров.
    /// </summary>
    [Required(ErrorMessage = "Kafka:BootstrapServers должен быть задан.")]
    public string BootstrapServers { get; init; } = string.Empty;

    /// <summary>
    /// Имя группы потребителей сервиса событий.
    /// </summary>
    [Required(ErrorMessage = "Kafka:ConsumerGroup должен быть задан.")]
    public string ConsumerGroup { get; init; } = string.Empty;
}
