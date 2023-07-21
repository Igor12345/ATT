using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;


public record FilledBufferPackage(byte[] RowData, ExpandingStorage<Line> ParsedRecords, int Id, bool IsLastPackage, int WrittenBytes);
//todo rename to message?
public class ReadyForExtractionPackage
{

    public ReadyForExtractionPackage(FilledBufferPackage filledBufferPackage, Range lineLocation)
    {
        
        RowData = filledBufferPackage.RowData;
        IsLastPackage = filledBufferPackage.IsLastPackage;
        ParsedRecords = filledBufferPackage.ParsedRecords;
        Id = filledBufferPackage.Id;
        
        //todo + MaxLineLength!!!
        LineData = RowData.AsMemory(lineLocation);
    }
    public ReadyForExtractionPackage(byte[] rowData, ExpandingStorage<Line> linesStorage, int id, bool isLast, int startOfLine)
    {
        RowData = rowData;
        IsLastPackage = isLast;
        ParsedRecords = linesStorage;
        Id = id;
        LineData = rowData.AsMemory(startOfLine);
    }

    public byte[] RowData { get; init; }
    public ExpandingStorage<Line> ParsedRecords { get; init; }
    public int Id { get; init; }
    public bool IsLastPackage { get; init; }

    private static readonly ReadyForExtractionPackage _emptyPackage =
        new(Array.Empty<byte>(), new ExpandingStorage<Line>(0), -1, false, 0);

    public Memory<byte> LineData { get; init; }

    public static ReadyForExtractionPackage Empty => _emptyPackage;
    public int StartOfLine { get; init; }
    public int WrittenBytesLength { get; init; }
}

public record PreReadPackage(
    byte[] RemainedBytes,
    int RemainedBytesLength,
    int Id,
    bool IsLastPackage)
{
    public static PreReadPackage LastPackage(int packageNumber) => new(Array.Empty<byte>(), 0, packageNumber, true);
};

public record SortingPhasePackage(byte[] RowData,
    Memory<byte> LineData,
    int OccupiedLength,
    ExpandingStorage<Line> ParsedRecords,
    int LinesNumber,
    int Id,
    bool IsLastPackage);

public record AfterSortingPhasePackage(
    Line[] SortedLines,
    byte[] RowData, 
    ExpandingStorage<Line> ParsedRecords, 
    int LinesNumber, 
    int Id,
    bool IsLastPackage);