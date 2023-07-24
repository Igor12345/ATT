using System.Threading.Channels;
using Infrastructure.Parameters;

namespace Infrastructure.Concurrency;

public class AwaitingQueueChannels<T>
{
    private readonly Channel<T> _queue;
    private readonly CancellationToken _token;


    public AwaitingQueueChannels(int capacity, CancellationToken token)
    {
        _token = Guard.NotNull(token);
        _queue = Channel.CreateBounded<T>(capacity);
    }

    public ValueTask EnqueueAsync(T item)
    {
        return _queue.Writer.WriteAsync(item, _token);
    }

    public ValueTask<T> DequeueAsync()
    {
        return _queue.Reader.ReadAsync(_token);
    }

    public void Complete()
    {
        _queue.Writer.Complete();
    }
}