using System.Collections.Concurrent;
using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePool : IDisposable
{
//todo move?
    private readonly record struct OrderedBuffer(int Index, byte[] Buffer, int WrittenBytes);
    
    private readonly SemaphoreSlim _semaphore;
    private readonly IBytesProducer _bytesProducer;
    private readonly int _inputBuffersLength;
    private readonly int _recordChunksLength;
    // private readonly ConcurrentQueue<OrderedBuffer> _filledBuffers;
    private readonly ConcurrentStack<byte[]> _emptyBuffers;
    private readonly ConcurrentStack<ExpandingStorage<Line>> _lineStorages;
    private SpinLock _lock;
    private SpinLock _readLock;
    private readonly AwaitingQueue<OrderedBuffer> _filledBuffers;
    private CancellationTokenSource? _cts;

    public SortingPhasePool(int numberOfBuffers, int inputBuffersLength, int recordChunksLength, IBytesProducer bytesProducer)
    {
        _bytesProducer = Guard.NotNull(bytesProducer);
        _lock = new SpinLock();
        _readLock = new SpinLock();
        _ = Guard.Positive(numberOfBuffers);
        _inputBuffersLength = Math.Min(Guard.Positive(inputBuffersLength), Array.MaxLength);
        _recordChunksLength = Math.Min(Guard.Positive(recordChunksLength), Array.MaxLength);
        _emptyBuffers = new ConcurrentStack<byte[]>();
        _filledBuffers = new AwaitingQueue<OrderedBuffer>();
        _semaphore = new SemaphoreSlim(numberOfBuffers, numberOfBuffers);
        //The most likely scenario is that storages for recognized lines will be returned
        //much faster than the buffers for the row bytes. In any case,
        //their size is much smaller than the size of the bytes array,
        //so creating a few extra storages won't be much harm.
        _lineStorages = new ConcurrentStack<ExpandingStorage<Line>>();
    }

    public void Run(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                if (state == null)
                    throw new NullReferenceException(nameof(state));
                var pool = (SortingPhasePool)state;

                int index = 0;
                do
                {
                    byte[] buffer = await pool.TryRentNextEmptyBufferAsync();
                    bool lockTaken = false;
                    try
                    {
                        pool._readLock.Enter(ref lockTaken);
                        ReadingResult readingResult =
                            await pool._bytesProducer.ProvideBytesAsync(buffer).ConfigureAwait(false);
                        //todo process error
                        
                        //todo
                        Console.WriteLine(
                            $"The next buffer hes been read, it contains {readingResult.ActuallyRead} bytes. Result: {readingResult.Success}, ");
                        
                        OrderedBuffer nextBuffer = new OrderedBuffer(index++, buffer, readingResult.ActuallyRead);
                        pool._filledBuffers.Enqueue(nextBuffer);
                        if (readingResult.ActuallyRead == 0)
                            break;
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
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            OrderedBuffer nextBuffer = await _filledBuffers.DequeueAsync();
            if (nextBuffer.Index != lastIndex + 1)
            {
                throw new InvalidOperationException(
                    $"Wrong order of prepared buffers, expected buffer for the package {lastIndex}, but was: {nextBuffer.Index}.");
            }

            ExpandingStorage<Line> linesStorage = RentLinesStorage();

            //todo return another entity, last parameter is useful
            return new FilledBufferPackage(nextBuffer.Buffer, linesStorage, nextBuffer.Index,
                nextBuffer.WrittenBytes == 0, nextBuffer.WrittenBytes);
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
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