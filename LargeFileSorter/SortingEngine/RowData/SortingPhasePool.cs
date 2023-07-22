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
    // private SpinLock _lock;
    private readonly int _numberOfBuffers;

    private readonly AsyncLock _lock;
    private SpinLock _readLock;
    private AwaitingQueueChannels<OrderedBuffer>? _filledBuffers;
    private int _createdBuffers = 0;
    private CancellationTokenSource? _cts;


    public SortingPhasePool(int numberOfBuffers, int inputBuffersLength, int recordChunksLength,
        IBytesProducer bytesProducer)
    {
        _bytesProducer = Guard.NotNull(bytesProducer);
        _lock = new AsyncLock();
        _readLock = new SpinLock();
        _numberOfBuffers = Guard.Positive(numberOfBuffers);
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
        //todo
        Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId}) Entering Pool.Run");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                if (state == null)
                    throw new NullReferenceException(nameof(state));
                var pool = (SortingPhasePool)state;

                int index = 0;
                do
                {
                    //todo
                    Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId}) Inside Run loop");
                    byte[] buffer = await pool.TryRentNextEmptyBufferAsync(); //without .ConfigureAwait(false) either replace lock on asyncLock 
                    bool lockTaken = false;
                    try
                    {
                        pool._readLock.Enter(ref lockTaken);
                        
                        //todo
                        Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId}) Inside Run loop, after lock for {index}, waiting for bytes");
                        
                        ReadingResult readingResult = pool._bytesProducer.ProvideBytes(buffer);
                        //todo process error

                        //todo
                        Console.WriteLine(
                            $"({Thread.CurrentThread.ManagedThreadId}) The next buffer hes been read, it contains {readingResult.ActuallyRead} bytes. Result: {readingResult.Success}, ");

                        OrderedBuffer nextBuffer = new OrderedBuffer(index++, buffer, readingResult.ActuallyRead);
                        await pool._filledBuffers.Enqueue(nextBuffer);
                        
                        //todo
                        Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId}) Inside Run loop Filled buffer added to queue index: {nextBuffer.Index}");
                        if (readingResult.ActuallyRead == 0)
                        {
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
        
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) Entering TryAcquireNextFilledBufferAsync index: {lastIndex}");

        using var _ = await _lock.LockAsync();
        //todo use async lock
        // _lock.Enter(ref lockTaken);

        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) TryAcquireNextFilledBufferAsync waiting for nextBuffer index: {lastIndex}");
        OrderedBuffer nextBuffer = await _filledBuffers.DequeueAsync();
        
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) TryAcquireNextFilledBufferAsync nextBuffer received from queue, index: {lastIndex}");
        
        if (nextBuffer.Index != lastIndex + 1)
        {
            throw new InvalidOperationException(
                $"Wrong order of prepared buffers, expected buffer for the package {lastIndex}, but was: {nextBuffer.Index}.");
        }

        ExpandingStorage<Line> linesStorage = RentLinesStorage();

        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) TryAcquireNextFilledBufferAsync nextBuffer acquired, return index: {lastIndex} - {nextBuffer.WrittenBytes}");
        //todo return another entity, last parameter is useful
        return new FilledBufferPackage(nextBuffer.Buffer, linesStorage, nextBuffer.Index,
            nextBuffer.WrittenBytes == 0, nextBuffer.WrittenBytes);
    }

    public void ReleaseBuffer(ExpandingStorage<Line> expandingStorage)
    {
        _lineStorages.Push(expandingStorage);
    }

    public void ReuseBuffer(byte[] buffer)
    {
        
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) Entering ReuseBuffer, _emptyBuffers: {_emptyBuffers.Count} ");
        
        _emptyBuffers.Push(buffer);
        _semaphore.Release();
        
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) ReuseBuffer, semaphore.Released _emptyBuffers: {_emptyBuffers.Count} ");
    }

    private async Task<byte[]> TryRentNextEmptyBufferAsync()
    {
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) Entering TryRentNextEmptyBufferAsync, waiting for semaphore ");
        await _semaphore.WaitAsync();
        
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) Entering TryRentNextEmptyBufferAsync, semaphore passed");
        
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

        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId}) TryRentNextEmptyBufferAsync, buffer received, existed in pool: {bufferExists}, created buffers {_createdBuffers}  ");
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