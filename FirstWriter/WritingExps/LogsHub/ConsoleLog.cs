using System.Threading.Channels;

namespace LogsHub;

internal class ConsoleLog
{
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationToken _cancellationToken;
    private volatile int _stopMarker;

    public static ConsoleLog Create(Channel<LogEntry> channel, CancellationToken cancellationToken)
    {
        ConsoleLog instance = new ConsoleLog(channel, cancellationToken);
        instance.Work();
        return instance;
    }

    private ConsoleLog(Channel<LogEntry> channel, CancellationToken cancellationToken)
    {
        _channel = channel;
        _cancellationToken = cancellationToken;
    }

    public void Stop()
    {
        Interlocked.Increment(ref _stopMarker);
    }

    private void Work()
    {
        while (Interlocked.CompareExchange(ref _stopMarker, 1, 0) == 0)
        {
            if (_cancellationToken.IsCancellationRequested)
                break;

            if (_channel.Reader.TryRead(out var entry))
            {
                Console.WriteLine(entry.Message);
            }
        }
    }
}