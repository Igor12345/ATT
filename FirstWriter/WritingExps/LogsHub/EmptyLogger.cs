namespace LogsHub;

public class EmptyLogger : ILogger
{
    public ValueTask LogAsync(LogEntry record)
    {
        return ValueTask.CompletedTask;
    }
}