namespace Infrastructure.Concurrency;

public class AwaitingQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private SpinLock _lock = new();

    public void Enqueue(T item)
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            
            //todo fix issue with several items in and one out
            _queue.Enqueue(item);
            _semaphore.Release();
        }
        finally
        {
            if(lockTaken)
                _lock.Exit(false);
        }
    }

    public async Task<T> DequeueAsync()
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            if (_queue.Count == 0)
                await _semaphore.WaitAsync();
            return _queue.Dequeue();
        }
        finally
        {
            if (lockTaken)
                _lock.Exit(false);
        }
    } 
}