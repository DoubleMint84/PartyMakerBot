using System.ComponentModel.DataAnnotations;

namespace PartyMakerBot.Model;

public class DownloadedUrl
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Url { get; set; } = null!;

    // Путь к локальному файлу (или другая метаинформация)
    [StringLength(300)]
    public string? LocalPath { get; set; }

    public DateTimeOffset DownloadedAt { get; set; }

    // Навигационное свойство — у одного DownloadedUrl может быть много QueueItem
    public ICollection<QueueItem> QueueItems { get; set; } = new List<QueueItem>();
}