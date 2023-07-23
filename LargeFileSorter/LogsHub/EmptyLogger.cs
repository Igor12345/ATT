namespace LogsHub;

public class EmptyLogger : ILogger
{
    public bool Log(LogEntry record) => true;

    public ValueTask LogAsync(LogEntry record) => ValueTask.CompletedTask;

    public ValueTask LogAsync(Func<LogEntry> getRecord) => ValueTask.CompletedTask;
}