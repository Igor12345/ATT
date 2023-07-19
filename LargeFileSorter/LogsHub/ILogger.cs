namespace LogsHub;

public interface ILogger
{
    ValueTask LogAsync(LogEntry record);
    ValueTask LogAsync(Func<LogEntry> getRecord);
}