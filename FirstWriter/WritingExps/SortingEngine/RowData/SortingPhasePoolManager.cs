using System.Buffers;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePoolManager : IAsyncObserver<PreReadPackage>, IAsyncObserver<AfterSortingPhasePackage>,
    IDisposable
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly int _inputBuffersLength;
    private volatile int _packageNumber = -1;
    private readonly int _recordChunksLength;
    private readonly CancellationToken _cancellationToken;
    private readonly byte[][] _buffers;
    private readonly ConcurrentStack<ExpandingStorage<LineMemory>> _lineStorages;
    private int _currentBuffer;
    private SpinLock _lock;

    public SortingPhasePoolManager(int numberOfBuffers, int inputBuffersLength, int recordChunksLength,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _lock = new SpinLock();
        _ = Guard.Positive(numberOfBuffers);
        _inputBuffersLength = Guard.Positive(inputBuffersLength);
        _recordChunksLength = Guard.Positive(recordChunksLength);
        _logger = Guard.NotNull(logger);
        _cancellationToken = Guard.NotNull(cancellationToken);
        _buffers = new byte[numberOfBuffers][];
        _semaphore = new SemaphoreSlim(numberOfBuffers, numberOfBuffers);
        //The most likely scenario is that storages for recognized lines will be returned
        //much faster than the buffers for the row bytes. In any case,
        //their size is much smaller than the size of the bytes array,
        //so creating a few extra storages won't be much harm.
        _lineStorages = new ConcurrentStack<ExpandingStorage<LineMemory>>();
    }
    
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }

    private readonly SimpleAsyncSubject<ReadingPhasePackage> _loadNextChunkSubject =
        new SequentialSimpleAsyncSubject<ReadingPhasePackage>();

    public IAsyncObservable<ReadingPhasePackage> LoadNextChunk => _loadNextChunkSubject;

    private async ValueTask<(bool ready, ReadingPhasePackage package)> TryAcquireNext()
    {
        await Log($"Trying acquire new bytes buffer, last package was {_packageNumber}");
        await _semaphore.WaitAsync(_cancellationToken);

        byte[]?[] buffers = _buffers;
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            if (_currentBuffer < _buffers.Length)
            {
                IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(_inputBuffersLength);
                // owner.Memory
                owner.Dispose();
                //todo check
                //it has a default max array length, equal to 2^20 (1024*1024 = 1 048 576)
                //https://adamsitnik.com/Array-Pool/
                buffers[_currentBuffer] ??= ArrayPool<byte>.Shared.Rent(_inputBuffersLength);
                byte[]? buffer = buffers[_currentBuffer++];
                ExpandingStorage<LineMemory> linesStorage = RentLinesStorage();

                await Log($"New bytes buffer has been rented, this package is {_packageNumber + 1}");
                return (true,
                    new ReadingPhasePackage(buffer!, linesStorage, Interlocked.Increment(ref _packageNumber)));
            }
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }

        return (false, ReadingPhasePackage.Empty);
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

    public void Return(ExpandingStorage<LineMemory> storage)
    {
        _lineStorages.Push(storage);
    }

    public async ValueTask OnNextAsync(PreReadPackage package)
    {
        Console.WriteLine($"-> In SortingPhasePoolManager OnNextAsync PreReadPackage for {package.PackageNumber}, is last: {package.IsLastPackage}");
        
        if(package.IsLastPackage)
            return;
        
        (bool ready, ReadingPhasePackage initialPackage) = await TryAcquireNext();
        if (!ready)
            await _loadNextChunkSubject.OnErrorAsync(new InvalidOperationException("Can't acquire free array"));

        ReadingPhasePackage nextPackage = initialPackage with { PrePopulatedBytesLength = package.RemainedBytesLength };
        package.RemainedBytes.CopyTo(nextPackage.RowData, 0);

        if (package.RemainedBytes.Length > 0)
            ArrayPool<byte>.Shared.Return(package.RemainedBytes);

        Console.WriteLine($"-> In SortingPhasePoolManager before _loadNextChunkSubject.OnNextAsync for {nextPackage.PackageNumber}, is last: {package.IsLastPackage}");

        await _loadNextChunkSubject.OnNextAsync(nextPackage);
    }

    public async ValueTask OnNextAsync(AfterSortingPhasePackage package)
    {
        Console.WriteLine($"-->! In SortingPhasePoolManager OnNextAsync AfterSortingPhasePackage for {package.PackageNumber}");
        await Log($"Returning AfterSortingPhasePackage {package.PackageNumber}, now the package is: {_packageNumber}");
        
        ReleaseBuffer(package.ParsedRecords);
        ReleaseBuffer(package.RowData);
        ArrayPool<LineMemory>.Shared.Return(package.SortedLines);

        if (package.IsLastPackage)
        {
            Console.WriteLine($"-> In SortingPhasePoolManager before _loadNextChunkSubject.OnCompletedAsync for {package.PackageNumber}, is last: {package.IsLastPackage}");
            await _loadNextChunkSubject.OnCompletedAsync();
        }
    }

    private void ReleaseBuffer(ExpandingStorage<LineMemory> expandingStorage)
    {
        _lineStorages.Push(expandingStorage);
    }

    private void ReleaseBuffer(byte[] buffer)
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            if (_currentBuffer != 0)
            {
                _buffers[--_currentBuffer] = buffer;
                _semaphore.Release();
            }
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
    }

    public ValueTask OnErrorAsync(Exception error)
    {
        return _loadNextChunkSubject.OnCompletedAsync();
    }

    public ValueTask OnCompletedAsync()
    {
        Console.WriteLine($"<---->! In SortingPhasePoolManager OOnCompletedAsync thread: {Thread.CurrentThread.ManagedThreadId}");
        
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        foreach (byte[] buffer in _buffers)
        {
            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }

        Array.Clear(_buffers);

        while (_lineStorages.TryPop(out var storage))
        {
            storage.Dispose();
        }
    }

    public async ValueTask LetsStart()
    {
        var (ready, package) = await TryAcquireNext();
        if (ready)
        {
            Console.WriteLine($"-> In SortingPhasePoolManager LetsStart before _loadNextChunkSubject.OnNextAsync for {package.PackageNumber}, is last: {package.IsLastPackage}");
            await _loadNextChunkSubject.OnNextAsync(package);
        }
        else
        {
            throw new InvalidOperationException("Something wrong. Nobody should be here");
        }
    }
}