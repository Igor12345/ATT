using System.Collections.Concurrent;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePool : IDisposable
{
    private readonly IBytesProducer _bytesProducer;

    private readonly record struct OrderedBuffer(int Index, byte[] Buffer);
    
    private readonly SemaphoreSlim _semaphore;
    private readonly int _inputBuffersLength;
    private volatile int _packageNumber = -1;
    private readonly int _recordChunksLength;
    private readonly ConcurrentQueue<OrderedBuffer> _filledBuffers;
    private readonly ConcurrentStack<byte[]> _emptyBuffers;
    private readonly ConcurrentStack<ExpandingStorage<Line>> _lineStorages;
    private SpinLock _lock;
    private SpinLock _readLock;

    public SortingPhasePool(int numberOfBuffers, int inputBuffersLength, int recordChunksLength, IBytesProducer bytesProducer)
    {
        _bytesProducer = Guard.NotNull(bytesProducer);
        _lock = new SpinLock();
        _readLock = new SpinLock();
        _ = Guard.Positive(numberOfBuffers);
        _inputBuffersLength = Math.Min(Guard.Positive(inputBuffersLength), Array.MaxLength);
        _recordChunksLength = Math.Min(Guard.Positive(recordChunksLength), Array.MaxLength);
        _emptyBuffers = new ConcurrentStack<byte[]>();
        _semaphore = new SemaphoreSlim(numberOfBuffers, numberOfBuffers);
        //The most likely scenario is that storages for recognized lines will be returned
        //much faster than the buffers for the row bytes. In any case,
        //their size is much smaller than the size of the bytes array,
        //so creating a few extra storages won't be much harm.
        _lineStorages = new ConcurrentStack<ExpandingStorage<Line>>();
    }

    public void Run(CancellationToken cancellationToken)
    {
        Task<Task<bool>> t = Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                if (state == null)
                    throw new NullReferenceException(nameof(state));
                var pool = (SortingPhasePool)state;

                int index = 0;
                do
                {
                    byte[] buffer = await pool.TryRentNextAsync();
                    bool lockTaken = false;
                    try
                    {
                        pool._readLock.Enter(ref lockTaken);
                        ReadingResult res = await pool._bytesProducer.WriteBytesToBufferAsync(buffer);
                        if (res.ActuallyRead == 0)
                            break;
                        OrderedBuffer nextBuffer = new OrderedBuffer(index++, buffer);
                        pool._filledBuffers.Enqueue(nextBuffer);
                    }
                    finally
                    {
                        if (lockTaken) pool._readLock.Exit(false);
                    }
                } while (true);

                return true;
            }, this, cancellationToken,
            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);
    }

    private async Task<byte[]> TryRentNextAsync()
    {
        await _semaphore.WaitAsync();
        
        bool lockTaken = false;
        try
        {
            
            //todo do we need a lock here?
            _lock.Enter(ref lockTaken);
            bool bufferExists = _emptyBuffers.TryPop(out byte[]? buffer);
            if (!bufferExists)
            {
                //The semaphore ensures that we don't exceed the allowed number of existing buffers.
                //It is safe a new buffer here
                buffer = new byte[_inputBuffersLength];
                //Array pool holds memory and refuses to release it
                // buffer = ArrayPool<byte>.Shared.Rent(_inputBuffersLength);
            }

            return buffer!;
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
    }
    
    public async Task<ReadingPhasePackage> TryAcquireNextAsync(int lastIndex)
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            bool bufferExists = _filledBuffers.TryDequeue(out byte[]? buffer);
            if (!bufferExists)
            {
                //The semaphore ensures that we don't exceed the allowed number of existing buffers.
                //It is safe a new buffer here
                buffer = new byte[_inputBuffersLength];
                //Array pool holds memory and refuses to release it
                // buffer = ArrayPool<byte>.Shared.Rent(_inputBuffersLength);
            }
            ExpandingStorage<Line> linesStorage = RentLinesStorage();

            return new ReadingPhasePackage(buffer!, linesStorage, Interlocked.Increment(ref _packageNumber), false);
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
    }

    private ExpandingStorage<Line> RentLinesStorage()
    {
        ExpandingStorage<Line> storage;
        while (!_lineStorages.TryPop(out storage!))
        {
            _lineStorages.Push(new ExpandingStorage<Line>(_recordChunksLength));
        }

        storage.Clear();
        return storage;
    }

    public void ReleaseBuffer(ExpandingStorage<Line> expandingStorage)
    {
        _lineStorages.Push(expandingStorage);
    }

    public void ReuseBuffer(byte[] buffer)
    {
        _emptyBuffers.Push(buffer);
        _semaphore.Release();
    }

    public void Dispose()
    {
        while (_lineStorages.TryPop(out var storage))
        {
            storage.Dispose();
        }
    }
}