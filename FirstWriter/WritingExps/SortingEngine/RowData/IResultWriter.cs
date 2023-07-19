using SortingEngine.Entities;

namespace SortingEngine.RowData;

public interface ILinesWriter
{
    Result WriteRecords(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source);
}