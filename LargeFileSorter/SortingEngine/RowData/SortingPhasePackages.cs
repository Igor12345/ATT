using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public record ReadingPhasePackage(byte[] RowData, ExpandingStorage<Line> ParsedRecords, int PackageNumber,
    bool IsLastPackage)
{
    private static readonly ReadingPhasePackage _emptyPackage =
        new(Array.Empty<byte>(), new ExpandingStorage<Line>(0), -1, false);

    public static ReadingPhasePackage Empty => _emptyPackage;
    public int PrePopulatedBytesLength { get; init; }
    public int WrittenBytesLength { get; init; }
}

public record PreReadPackage(
    byte[] RemainedBytes,
    int RemainedBytesLength,
    int PackageNumber,
    bool IsLastPackage)
{
    public static PreReadPackage LastPackage(int packageNumber) => new(Array.Empty<byte>(), 0, packageNumber, true);
};

public record SortingPhasePackage(
    byte[] RowData,
    int OccupiedLength,
    ExpandingStorage<Line> ParsedRecords,
    int LinesNumber, 
    int PackageNumber,
    bool IsLastPackage);

public record AfterSortingPhasePackage(
    Line[] SortedLines,
    byte[] RowData, 
    ExpandingStorage<Line> ParsedRecords, 
    int LinesNumber, 
    int PackageNumber,
    bool IsLastPackage);