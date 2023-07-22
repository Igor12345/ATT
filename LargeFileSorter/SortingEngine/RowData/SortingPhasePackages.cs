using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

//todo move?
public readonly record struct OrderedBuffer(int Index, byte[] Buffer, int WrittenBytes);
public abstract record BasePackage(int Id, bool IsLastPackage, byte[] RowData, ExpandingStorage<Line> ParsedRecords);

public record FilledBufferPackage : BasePackage
{
    public FilledBufferPackage(OrderedBuffer basePackage, int writtenBytes, ExpandingStorage<Line> ParsedRecords) :
        base(basePackage.Index,
            writtenBytes == 0, basePackage.Buffer, ParsedRecords)
    {
        WrittenBytesLength = Guard.NotNegative(writtenBytes);
    }

    protected FilledBufferPackage(int Id, bool IsLastPackage, byte[] RowData, ExpandingStorage<Line> ParsedRecords, int writtenBytes) :
        base(Id, IsLastPackage, RowData, ParsedRecords)
    {
        WrittenBytesLength = Guard.NotNegative(writtenBytes);
    }

    //todo record's constructor
    // protected FilledBufferPackage(FilledBufferPackage source, int writtenBytes) :
    //     this((BasePackage)source, writtenBytes)
    // {
    //     WrittenBytesLength = Guard.NotNegative(writtenBytes);
    // }

    public int WrittenBytesLength { get; }
}

public record ReadyForExtractionPackage : FilledBufferPackage
{
    public ReadyForExtractionPackage(FilledBufferPackage bufferPackage, Range lineLocation) : base(bufferPackage)
    {
        LineData = RowData.AsMemory(lineLocation);
    }

    protected ReadyForExtractionPackage(ReadyForExtractionPackage source) : base(source)
    {
        LineData = source.LineData;
    }

    private ReadyForExtractionPackage(int Id, bool IsLastPackage, byte[] RowData, ExpandingStorage<Line> ParsedRecords,
        int writtenBytes) : base(Id, IsLastPackage, RowData, ParsedRecords, writtenBytes)
    {
    }
    public Memory<byte> LineData { get; }

    private static readonly ReadyForExtractionPackage _emptyPackage =
        new(-1, false, Array.Empty<byte>(), new ExpandingStorage<Line>(0), 0);
    public static ReadyForExtractionPackage Empty => _emptyPackage;
}

//todo rename to message?
public record SortingPhasePackage : ReadyForExtractionPackage
{
    public SortingPhasePackage(ReadyForExtractionPackage extractionPackage, int linesNumber) : base(extractionPackage)
    {
        LinesNumber = linesNumber;
    }

    protected SortingPhasePackage(SortingPhasePackage source) : base(source)
    {
        LinesNumber = source.LinesNumber;
    }

    public int LinesNumber { get; }
}

public record AfterSortingPhasePackage : SortingPhasePackage
{
    public AfterSortingPhasePackage(SortingPhasePackage sortingPhasePackage, Line[] sortedLines):base(sortingPhasePackage)
    {
        SortedLines = sortedLines;
    }
    
    public Line[] SortedLines { get; }
}

public record AfterSavingBufferPackage : BasePackage
{
    public AfterSavingBufferPackage(AfterSortingPhasePackage afterSortingPhasePackage) : base(
        afterSortingPhasePackage.Id, afterSortingPhasePackage.IsLastPackage, afterSortingPhasePackage.RowData,
        afterSortingPhasePackage.ParsedRecords)
    {
        BufferForSortedLines = afterSortingPhasePackage.SortedLines;
    }

    public Line[] BufferForSortedLines { get; }
}

public record PreReadPackage(
    int Id,
    bool IsLastPackage,
    byte[] RemainedBytes,
    int RemainedBytesLength)
{
    public static PreReadPackage LastPackage(int packageNumber) => new(packageNumber, true, Array.Empty<byte>(), 0);
}

public class ReadyForExtractionPackageCl
{

    public ReadyForExtractionPackageCl(FilledBufferPackage filledBufferPackage, Range lineLocation)
    {
        
        RowData = filledBufferPackage.RowData;
        IsLastPackage = filledBufferPackage.IsLastPackage;
        ParsedRecords = filledBufferPackage.ParsedRecords;
        Id = filledBufferPackage.Id;
        
        //todo + MaxLineLength!!!
        LineData = RowData.AsMemory(lineLocation);
    }
    public ReadyForExtractionPackageCl(byte[] rowData, ExpandingStorage<Line> linesStorage, int id, bool isLast, int startOfLine)
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

    public Memory<byte> LineData { get; init; }

    public int StartOfLine { get; init; }
    public int WrittenBytesLength { get; init; }
}