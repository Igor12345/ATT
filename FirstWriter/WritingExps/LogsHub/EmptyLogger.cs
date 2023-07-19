namespace LogsHub;

public class EmptyLogger : ILogger
{
    public ValueTask LogAsync(LogEntry record)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask LogAsync(Func<LogEntry> getRecord)
    {
        return ValueTask.CompletedTask;
    }
}