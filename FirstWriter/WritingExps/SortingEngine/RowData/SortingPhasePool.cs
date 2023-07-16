using System.Buffers;
using System.Collections.Concurrent;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePool : IDisposable
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly int _inputBuffersLength;
    private volatile int _packageNumber = -1;
    private readonly int _recordChunksLength;
    private readonly ConcurrentStack<byte[]> _buffers;
    private readonly ConcurrentStack<ExpandingStorage<LineMemory>> _lineStorages;
    private SpinLock _lock;

    public SortingPhasePool(int numberOfBuffers, int inputBuffersLength, int recordChunksLength,
        ILogger logger)
    {
        _lock = new SpinLock();
        _ = Guard.Positive(numberOfBuffers);
        _inputBuffersLength = Math.Min(Guard.Positive(inputBuffersLength), Array.MaxLength);
        _recordChunksLength = Math.Min(Guard.Positive(recordChunksLength), Array.MaxLength);
        _logger = Guard.NotNull(logger);
        _buffers = new ConcurrentStack<byte[]>();
        _semaphore = new SemaphoreSlim(numberOfBuffers, numberOfBuffers);
        //The most likely scenario is that storages for recognized lines will be returned
        //much faster than the buffers for the row bytes. In any case,
        //their size is much smaller than the size of the bytes array,
        //so creating a few extra storages won't be much harm.
        _lineStorages = new ConcurrentStack<ExpandingStorage<LineMemory>>();
    }
    
    //in a real project, working with logs will look completely different
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
    
    public async Task<ReadingPhasePackage> TryAcquireNextAsync()
    {
        //todo remove semaphore
        await Log($"Trying acquire new bytes buffer, last package was {_packageNumber}, semaphore: {_semaphore.CurrentCount}");
        await _semaphore.WaitAsync();
        
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            var bufferExists = _buffers.TryPop(out var buffer);
            if (!bufferExists)
            {
                //The semaphore ensures that we don't exceed the allowed number of existing buffers.
                //It is safe a new buffer here
                buffer = ArrayPool<byte>.Shared.Rent(_inputBuffersLength);
            }
            ExpandingStorage<LineMemory> linesStorage = RentLinesStorage();

            await Log($"New bytes buffer has been rented, this package is {_packageNumber + 1}");
            return new ReadingPhasePackage(buffer!, linesStorage, Interlocked.Increment(ref _packageNumber));
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
    }

    private ExpandingStorage<LineMemory> RentLinesStorage()
    {
        ExpandingStorage<LineMemory> storage;
        while (!_lineStorages.TryPop(out storage!))
        {
            _lineStorages.Push(new ExpandingStorage<LineMemory>(_recordChunksLength));
        }

        return storage;
    }

    public void ReleaseBuffer(ExpandingStorage<LineMemory> expandingStorage)
    {
        _lineStorages.Push(expandingStorage);
    }

    public void ReleaseBuffer(byte[] buffer)
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            _buffers.Push(buffer);
            _semaphore.Release();
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
    }
    
    public void Dispose()
    {
        while (_buffers.TryPop(out var buffer))
        {
            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }

        while (_lineStorages.TryPop(out var storage))
        {
            storage.Dispose();
        }
    }
}