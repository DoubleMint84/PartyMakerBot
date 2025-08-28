using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PartyMakerBot.Model;

public class QueueItem
{
    [Key]
    public int Id {get; set; }
    
    [Required]
    public int Index { get; set;  } 
    
    [Required]
    [StringLength(300)]
    public string Url { get; set; } 
    public long OwnerId { get; set; }
    
    [StringLength(200)]
    public string OwnerDisplayName { get; set; }
    public DateTimeOffset AddedAt { get; set;  }
    public bool IsPlayed { get; set; }
    public int? DownloadedUrlId { get; set; }

    [ForeignKey(nameof(DownloadedUrlId))]
    public DownloadedUrl? DownloadedUrl { get; set; }
    
    [NotMapped] public string? FilePath => DownloadedUrl?.LocalPath;
    
    [NotMapped]
    public bool IsDownloaded => FilePath != null;
    public QueueItem(int index, string url, TelegramUser owner, DateTimeOffset addedAt)
    {
        Index = index;
        Url = url;
        OwnerId = owner.Id;
        OwnerDisplayName = owner.DisplayName;
        AddedAt = addedAt;
    }
    
    public QueueItem(int index, string url, long ownerId, string ownerDisplayName, DateTimeOffset addedAt)
    {
        Index = index;
        Url = url;
        OwnerId = ownerId;
        OwnerDisplayName = ownerDisplayName;
        AddedAt = addedAt;
    }
}