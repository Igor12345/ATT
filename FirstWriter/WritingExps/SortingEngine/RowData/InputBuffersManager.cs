using System.Buffers;
using System.Reactive.Subjects;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class InputBuffersManager : IAsyncObserver<byte[]>, IAsyncObserver<SortingCompletedEventArgs>, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _inputBuffersLength;
    private readonly int _recordChunksLength;
    private readonly CancellationToken _cancellationToken;
    private readonly byte[][] _buffers;
    private ExpandingStorage<LineMemory>[] _linesBuffers;
    private int _current;
    private SpinLock _lock;
    
    public InputBuffersManager(int numberOfBuffers, int inputBuffersLength, int recordChunksLength, CancellationToken cancellationToken)
    {
        _lock = new SpinLock();
        _ = Guard.Positive(numberOfBuffers);
        _inputBuffersLength = Guard.Positive(inputBuffersLength);
        _recordChunksLength = Guard.Positive(recordChunksLength);
        _cancellationToken = Guard.NotNull(cancellationToken);
        _buffers = new byte[numberOfBuffers][];
        _linesBuffers = new ExpandingStorage<LineMemory>[numberOfBuffers];
        _semaphore = new SemaphoreSlim(0, numberOfBuffers);
    }
    
    private readonly SimpleAsyncSubject<InputBuffer> _loadNextChunkSubject =
        new SequentialSimpleAsyncSubject<InputBuffer>();

    public IAsyncObservable<InputBuffer> LoadNextChunk => _loadNextChunkSubject;

    private async ValueTask<(bool ready, InputBuffer buffer)> TryAcquireNext()
    {
        await _semaphore.WaitAsync(_cancellationToken);
        
        byte[]?[] buffers = _buffers;
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            if (_current < _buffers.Length)
            {
                buffers[_current] ??= ArrayPool<byte>.Shared.Rent(_inputBuffersLength);
                byte[]? buffer = buffers[_current++];
                return (true, new InputBuffer(){Buffer = buffer!});
            }
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }

        return (false, new InputBuffer());
    }

    public async ValueTask OnNextAsync(byte[] value)
    {
        (bool ready, InputBuffer buffer) = await TryAcquireNext();
        if (!ready)
            await _loadNextChunkSubject.OnErrorAsync(new InvalidOperationException("Can't acquire free array"));
        
        //todo !!! real length can be different!!!
        buffer.UsedLength = value.Length;
        value!.CopyTo(buffer.Buffer, 0);
        await _loadNextChunkSubject.OnNextAsync(buffer);
    }

    public ValueTask OnNextAsync(SortingCompletedEventArgs value)
    {
        ReleaseBuffer(value.Sorted);
        return ValueTask.CompletedTask;
    }

    public void ReleaseBuffer(byte[] buffer)
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            
            if (_current != 0)
            {
                _buffers[--_current] = buffer;
                _semaphore.Release();
            }
        }
        finally
        {
            if (lockTaken) _lock.Exit(false);
        }
    }

    private void ReleaseBuffer(LineMemory[] sorted)
    {
        
    }

    public ValueTask OnErrorAsync(Exception error)
    {
        return _loadNextChunkSubject.OnCompletedAsync();
    }

    public ValueTask OnCompletedAsync()
    {
        return _loadNextChunkSubject.OnCompletedAsync();
    }

    public void Dispose()
    {
        foreach (byte[] buffer in _buffers)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async ValueTask LetsStart()
    {
        var (ready, buffer) = await TryAcquireNext();
        if (ready)
        {
            await _loadNextChunkSubject.OnNextAsync(buffer);
        }
        else
        {
            throw new InvalidOperationException("Something wrong. Nobody should be here");
        }
    }
}