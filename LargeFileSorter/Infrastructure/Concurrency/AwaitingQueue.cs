using System.Collections.Concurrent;

namespace Infrastructure.Concurrency;

public class AwaitingQueue1<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(0, 1);
    private SpinLock _lock = new();
    private AsyncLock _readersLock = new AsyncLock();
    private BlockingCollection<T> _collection = new(new ConcurrentQueue<T>());

    public void Enqueue(T item)
    {
        //todo
        Console.WriteLine($"--> ({Thread.CurrentThread.ManagedThreadId}) entering Enqueue with item, queue.Count: {_queue.Count}");
        bool lockTaken = false;
        try
        {
            //todo fix issue with several items in and one out
            // _lock.Enter(ref lockTaken);
            
            _queue.Enqueue(item);
            Thread.MemoryBarrier();
            _semaphore.Release();
            
            //todo
            Console.WriteLine($"--> ({Thread.CurrentThread.ManagedThreadId}) exit Enqueue semaphore released {_semaphore.CurrentCount}, queue.Count: {_queue.Count}");
        }
        finally
        {
            if(lockTaken)
                _lock.Exit(false);
        }
    }

    public async Task<T> DequeueAsync()
    {
        //todo
        Console.WriteLine($"<-- ({Thread.CurrentThread.ManagedThreadId}) entering DequeueAsync, queue.Count: {_queue.Count}");
        T? item;
        using (var _ = _readersLock.LockAsync())
        {

            while (!_queue.TryDequeue(out item))
            {
                //todo
                Console.WriteLine(
                    $"<-- ({Thread.CurrentThread.ManagedThreadId}) DequeueAsync waiting for semaphore, queue.Count: {_queue.Count}");
                await _semaphore.WaitAsync().ConfigureAwait(false);
                // if (_queue.TryPeek(out T? _))
                //     _semaphore.Release(); //next reader also can read
            }

            //todo
            Console.WriteLine(
                $"<-- ({Thread.CurrentThread.ManagedThreadId}) DequeueAsync successfully got item from queue, queue.Count: {_queue.Count}");
        }

        return item;
    } 
}