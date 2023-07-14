using System.Threading.Channels;

namespace LogsHub;

public class Logger
{
    private readonly Input _input;
    private readonly ConsoleLog _consoleLogger;

    private Logger(CancellationToken cancellationToken)
    {
        var inputChannel = Channel.CreateUnbounded<LogEntry>();
        _input = new Input(inputChannel);
        _consoleLogger = ConsoleLog.Create(inputChannel, cancellationToken);
    }

    public static Logger Create(CancellationToken cancellationToken)
    {
        Logger instance = new Logger(cancellationToken);
        return instance;
    }

    public ValueTask LogAsync(LogEntry record)
    {
        return _input.Log(record);
    }

    public async ValueTask StopAsync()
    {
        await Task.Delay(1);
        _consoleLogger.Stop();
    }
}