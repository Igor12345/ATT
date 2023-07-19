using System.Threading.Channels;

namespace LogsHub;

internal class Input
{
    public Input(Channel<LogEntry> input)
    {
        _input = input;
    }

    private readonly Channel<LogEntry> _input;
    
    public ValueTask Log(LogEntry record)
    {
        return _input.Writer.WriteAsync(record);
    }
}

public record struct LogEntry(string Message)
{
    public static implicit operator LogEntry(string value) => new(value);
}