namespace PartyMakerBot.Model;

public class QueueItem(int index, string url, TelegramUser owner, DateTimeOffset addedAt)
{
    public int Index { get; } = index;
    public string Url { get; } = url;
    public TelegramUser Owner { get; } = owner;
    public DateTimeOffset AddedAt { get; } = addedAt;
    public string? FilePath { get; private set; }
    public bool IsDownloaded => FilePath != null;
    
    public void MarkDownloaded(string path)
    {
        FilePath = path;
    }
}