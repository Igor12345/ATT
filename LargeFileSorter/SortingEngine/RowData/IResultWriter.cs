using SortingEngine.Entities;

namespace SortingEngine.RowData;

/// <summary>
/// An object implementing this interface will write a piece of information to a file and close it afterward.
/// This interface is used only inside the core part  
/// </summary>
public interface 
    
    IOneTimeLinesWriter
{
    Result WriteLines(string filePath, Line[] lines, int linesNumber, ReadOnlyMemory<byte> source);
    Task<Result> WriteLinesAsync(string filePath, Line[] lines, int linesNumber, ReadOnlyMemory<byte> source,
        CancellationToken token);
}

/// <summary>
/// An object implementing this interface can write several times in the same file.
/// This is a contract between this internal sorting engine and the external application sink of the data. 
/// </summary>
public interface ISeveralTimesLinesWriter : IDisposable, IAsyncDisposable
{
    Result WriteLines(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source);
    Task<Result> WriteLinesAsync(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source,
        CancellationToken token);
}