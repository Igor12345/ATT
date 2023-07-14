using System.Threading.Channels;

namespace LogsHub;

internal class ConsoleLog
{
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenSource _cts;

    public static ConsoleLog Create(Channel<LogEntry> channel, CancellationToken cancellationToken)
    {
        ConsoleLog instance = new ConsoleLog(channel, cancellationToken);
        instance.Run();
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
    }

    private void Run()
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
                Console.WriteLine(entry.Message);
            }
        }, new Tuple<ChannelReader<LogEntry>, CancellationToken>(reader, token), _cancellationToken);

    }
}