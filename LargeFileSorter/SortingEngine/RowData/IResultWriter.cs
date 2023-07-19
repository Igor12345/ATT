using SortingEngine.Entities;

namespace SortingEngine.RowData;

/// <summary>
/// An object implementing this interface will write a piece of information to a file and close it afterward. 
/// </summary>
public interface IOneTimeLinesWriter
{
    Result WriteRecords(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source);
    Task<Result> WriteRecordsAsync(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
        CancellationToken token);
}

/// <summary>
/// An object implementing this interface can write several times in the same file.
/// </summary>
public interface ISeveralTimesLinesWriter : IDisposable, IAsyncDisposable
{
    Result WriteRecords(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source);
    Task<Result> WriteRecordsAsync(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
        CancellationToken token);
}