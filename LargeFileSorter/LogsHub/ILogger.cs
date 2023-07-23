namespace LogsHub;

public interface ILogger
{
    bool Log(LogEntry record);
    ValueTask LogAsync(LogEntry record);
    ValueTask LogAsync(Func<LogEntry> getRecord);
}