namespace LogsHub;

public interface ILogger
{
    ValueTask LogAsync(LogEntry record);
}