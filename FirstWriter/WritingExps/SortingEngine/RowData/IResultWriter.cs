using SortingEngine.Entities;

namespace SortingEngine.RowData;

public interface ILinesWriter: IDisposable
{
    Result WriteRecords(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source);
}