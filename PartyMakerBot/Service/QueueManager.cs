using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using PartyMakerBot.Model;

namespace PartyMakerBot.Service;

public class QueueManager
{
    private readonly ConcurrentQueue<QueueItem> _queue = new();
    private int _counter = 0; 
    private readonly object _snapshotLock = new();
    
    public event Action? ItemEnqueued;

    public QueueItem Enqueue(string url, TelegramUser owner)
    {
        var index = Interlocked.Increment(ref _counter);
        var item = new QueueItem(index, url, owner, DateTimeOffset.UtcNow);
        _queue.Enqueue(item);
        ItemEnqueued?.Invoke();
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
            return true;
        }

        item = null;
        return false;
    }
    
    public (bool Success, string? ErrorMessage) TryRemoveByIndex(int index, TelegramUser requester)
    {
        lock (_snapshotLock)
        {
            var items = _queue.ToArray();
            var target = items.FirstOrDefault(i => i.Index == index);
            if (target == null)
                return (false, "Элемент не найден.");

            if (target.Owner.Id != requester.Id)
                return (false, "Можно удалять только собственные элементы.");
            
            var newQ = new ConcurrentQueue<QueueItem>();
            foreach (var i in items)
            {
                if (i.Index == index) continue;
                newQ.Enqueue(i);
            }
            
            while (_queue.TryDequeue(out _)) {}
            
            foreach (var i in newQ)
                _queue.Enqueue(i);

            return (true, null);
        }
    }
}