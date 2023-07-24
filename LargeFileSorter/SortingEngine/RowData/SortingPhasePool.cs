using System.Collections.Concurrent;
using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePool : IDisposable
{
    private readonly int _maxLineLength;


    private readonly SemaphoreSlim _semaphore;
    private readonly IBytesProducer _bytesProducer;
    private readonly int _inputBuffersLength;

    private readonly int _recordChunksLength;

    // private readonly ConcurrentQueue<OrderedBuffer> _filledBuffers;
    private readonly ConcurrentStack<byte[]> _emptyBuffers;
    private readonly ConcurrentStack<ExpandingStorage<Line>> _lineStorages;
    // private SpinLock _lock;
    private readonly int _numberOfBuffers;

    private readonly AsyncLock _lock;
    private SpinLock _readLock;
    private AwaitingQueueChannels<OrderedBuffer>? _filledBuffers;
    private int _createdBuffers;
    private CancellationTokenSource? _cts;


    public SortingPhasePool(int numberOfBuffers, int inputBuffersLength, int recordChunksLength, int maxLineLength,
        IBytesProducer bytesProducer)
    {
        _bytesProducer = Guard.NotNull(bytesProducer);
        _lock = new AsyncLock();
        _readLock = new SpinLock();
        _numberOfBuffers = Guard.Positive(numberOfBuffers);
        _maxLineLength = Guard.Positive(maxLineLength);
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
        _filledBuffers = new AwaitingQueueChannels<OrderedBuffer>(_numberOfBuffers, cancellationToken);
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                if (state == null)
                    throw new NullReferenceException(nameof(state));
                var pool = (SortingPhasePool)state;

                int index = 0;
                do
                {
                    byte[] buffer = await pool.TryRentNextEmptyBufferAsync(); //without .ConfigureAwait(false) either replace lock on asyncLock 
                    bool lockTaken = false;
                    try
                    {
                        pool._readLock.Enter(ref lockTaken);
                        
                        ReadingResult readingResult = pool._bytesProducer.ProvideBytes(buffer.AsMemory(pool._maxLineLength..));

                        //todo process error

                        OrderedBuffer nextBuffer = new OrderedBuffer(index++, buffer, readingResult.Length);
                        await pool._filledBuffers.EnqueueAsync(nextBuffer).ConfigureAwait(false);
                        
                        if (readingResult.Length == 0)
                        {
                            pool._filledBuffers.Complete();
                            //todo handle stop!!!
                            break;
                        }
                    }
                    finally
                    {
                        if (lockTaken) pool._readLock.Exit(false);
                    }
                } while (true);

                return true;
            }, this, _cts.Token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);
    }

    public async Task<FilledBufferPackage> TryAcquireNextFilledBufferAsync(int lastIndex)
    {
        if (_filledBuffers == null)
            throw new InvalidOperationException("Method Run should be called first");
        
        using var _ = await _lock.LockAsync();
        //todo use async lock
        // _lock.Enter(ref lockTaken);
        OrderedBuffer nextBuffer = await _filledBuffers.DequeueAsync().ConfigureAwait(false);
        
        if (nextBuffer.Index != lastIndex + 1)
        {
            throw new InvalidOperationException(
                $"Wrong order of prepared buffers, expected buffer for the package {lastIndex}, but was: {nextBuffer.Index}.");
        }

        ExpandingStorage<Line> linesStorage = RentLinesStorage();

        return new FilledBufferPackage(nextBuffer, nextBuffer.WrittenBytes, linesStorage);
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

    private async Task<byte[]> TryRentNextEmptyBufferAsync()
    {
        await _semaphore.WaitAsync();
        
        bool bufferExists = _emptyBuffers.TryPop(out byte[]? buffer);
        if (!bufferExists)
        {
            //The semaphore ensures that we don't exceed the allowed number of existing buffers.
            //It is safe a new buffer here
            buffer = new byte[_inputBuffersLength];
            Interlocked.Increment(ref _createdBuffers);
            //Array pool holds memory and refuses to release it
            // buffer = ArrayPool<byte>.Shared.Rent(_inputBuffersLength);
        }

        return buffer!;
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

    public void Dispose()
    {
        while (_lineStorages.TryPop(out var storage))
        {
            storage.Dispose();
        }

        _cts?.Cancel();
    }
}