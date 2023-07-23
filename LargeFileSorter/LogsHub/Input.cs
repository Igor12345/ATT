using System.Threading.Channels;

namespace LogsHub;

internal class Input
{
    public Input(Channel<LogEntry> input)
    {
        _input = input;
    }

    private readonly Channel<LogEntry> _input;
    
    public ValueTask LogAsync(LogEntry record)
    {
        return _input.Writer.WriteAsync(record);
    }

    public bool Log(LogEntry record)
    {
        return _input.Writer.TryWrite(record);
    }
}

public record struct LogEntry(string Message)
{
    public static implicit operator LogEntry(string value) => new(value);
}