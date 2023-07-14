using System.Threading.Channels;

namespace LogsHub;

internal class ConsoleLog
{
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationToken _cancellationToken;
    private volatile int _stopMarker;
    private readonly CancellationTokenSource _cts;

    public static ConsoleLog Create(Channel<LogEntry> channel, CancellationToken cancellationToken)
    {
        ConsoleLog instance = new ConsoleLog(channel, cancellationToken);
        instance.Work();
        return instance;
    }

    private ConsoleLog(Channel<LogEntry> channel, CancellationToken cancellationToken)
    {
        _channel = channel;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationToken = cancellationToken;
    }

    public void Stop()
    {
        _cts.Cancel();
        bool st = _cancellationToken.IsCancellationRequested;
        Interlocked.Increment(ref _stopMarker);
    }

    private void Work()
    {
        ChannelReader<LogEntry> reader = _channel.Reader;
        CancellationToken token = _cts.Token;

        Task.Factory.StartNew(static async (state) =>
        {
            if (state == null)
                throw new NullReferenceException(nameof(state));

            Tuple<ChannelReader<LogEntry>, CancellationToken> tuple =
                (Tuple<ChannelReader<LogEntry>, CancellationToken>)state;
            ChannelReader<LogEntry> reader = tuple.Item1;
            CancellationToken token = tuple.Item2;
            while (await reader.WaitToReadAsync(token))
            {
                var entry = await reader.ReadAsync(token);
                Console.WriteLine($"From task, thread - {Thread.CurrentThread.ManagedThreadId}: " + entry.Message);
                await Task.Delay(100);
            }
        }, new Tuple<ChannelReader<LogEntry>, CancellationToken>(reader, token), _cancellationToken);
        
        Task.Factory.StartNew(static async (state) =>
        {
            if (state == null)
                throw new NullReferenceException(nameof(state));

            Tuple<ChannelReader<LogEntry>, CancellationToken> tuple =
                (Tuple<ChannelReader<LogEntry>, CancellationToken>)state;
            ChannelReader<LogEntry> reader = tuple.Item1;
            CancellationToken token = tuple.Item2;
            while (await reader.WaitToReadAsync(token))
            {
                var entry = await reader.ReadAsync(token);
                Console.WriteLine($"From second task, thread - {Thread.CurrentThread.ManagedThreadId}: " + entry.Message);
                await Task.Delay(100);
            }
        }, new Tuple<ChannelReader<LogEntry>, CancellationToken>(reader, _cancellationToken), _cancellationToken);

        while (Interlocked.CompareExchange(ref _stopMarker, 1, 0) == 0)
        {
            if (_cancellationToken.IsCancellationRequested)
                break;

            if (_channel.Reader.TryRead(out var entry))
            {
                Console.WriteLine($"From loop, thread - {Thread.CurrentThread.ManagedThreadId}: " + entry.Message);
            }
        }
    }
}