using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using PartyMakerBot.Data;
using PartyMakerBot.Model;

namespace PartyMakerBot.Service;

public class QueueManager
{
    private readonly ConcurrentQueue<QueueItem> _queue = new();
    private int _counter = 0; 
    private readonly AppDbContext _db;
    private readonly object _snapshotLock = new();
    
    public event Action? ItemEnqueued;
    public event Action? QueueChanged;

    public QueueManager(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        EnsureDatabaseAndLoad();
    }
    
    private void EnsureDatabaseAndLoad()
    {
        lock (_snapshotLock)
        {
            _db.Database.EnsureCreated();
            var maxIndex = _db.QueueItems.Any() ? _db.QueueItems.Max(x => x.Index) : 0;
            _counter = maxIndex;
        
            var items = _db.QueueItems
                .Where(x => !x.IsPlayed)
                .OrderBy(x => x.Index)
                .ToList();
            
            foreach (var item in items)
                _queue.Enqueue(item);
        }
    }

    public QueueItem Enqueue(string url, TelegramUser owner)
    {
        var index = Interlocked.Increment(ref _counter);
        var item = new QueueItem(index, url, owner, DateTimeOffset.UtcNow);
        _db.QueueItems.Add(item);
        _db.SaveChanges();
        
        _queue.Enqueue(item);
        ItemEnqueued?.Invoke();
        QueueChanged?.Invoke();
        return item;
    }
    
    public List<QueueItem> GetSnapshot()
    {
        var arr = _queue.ToArray();
        return arr.OrderBy(i => i.Index).ToList();
    }

    public bool GetNext([MaybeNullWhen(false)] out QueueItem item)
    {
        if (_queue.TryPeek(out var result) && result.IsDownloaded && _queue.TryDequeue(out var queueItem))
        {
            item = queueItem;
            QueueChanged?.Invoke();
            return true;
        }

        item = null;
        return false;
    }
    
    public (bool Success, string? ErrorMessage) TryRemoveByIndex(int index, TelegramUser? requester = null)
    {
        lock (_snapshotLock)
        {
            var target = _db.QueueItems.FirstOrDefault(x => x.Index == index && !x.IsPlayed);
            if (target == null)
                return (false, "Элемент не найден.");

            if (requester != null && target.OwnerId != requester.Id)
                return (false, "Можно удалять только собственные элементы.");
            
            target.IsPlayed = true;
            _db.SaveChanges();
            
            var items = _queue.ToArray();
            var newQ = new ConcurrentQueue<QueueItem>();
            foreach (var i in items)
            {
                if (i.Index == index) continue;
                newQ.Enqueue(i);
            }
            
            while (_queue.TryDequeue(out _)) {}
            
            foreach (var i in newQ)
                _queue.Enqueue(i);

            QueueChanged?.Invoke();
            
            return (true, null);
        }
    }
    
    public bool MarkPlayed(int index)
    {
        lock (_snapshotLock)
        {
            var target = _db.QueueItems.FirstOrDefault(x => x.Index == index && !x.IsPlayed);
            if (target == null) return false;
            target.IsPlayed = true;
            _db.SaveChanges();
            return true;
        }
    }
}