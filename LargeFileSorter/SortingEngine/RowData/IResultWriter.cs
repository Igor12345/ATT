using SortingEngine.Entities;

namespace SortingEngine.RowData;

/// <summary>
/// An object implementing this interface will write a piece of information to a file and close it afterward. 
/// </summary>
public interface IOneTimeLinesWriter
{
    Result WriteRecords(string filePath, Line[] lines, int linesNumber, ReadOnlyMemory<byte> source);
    Task<Result> WriteRecordsAsync(string filePath, Line[] lines, int linesNumber, ReadOnlyMemory<byte> source,
        CancellationToken token);
}

/// <summary>
/// An object implementing this interface can write several times in the same file.
/// </summary>
public interface ISeveralTimesLinesWriter : IDisposable, IAsyncDisposable
{
    Result WriteRecords(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source);
    Task<Result> WriteRecordsAsync(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source,
        CancellationToken token);
}