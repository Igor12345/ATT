using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public record ReadingPhasePackage(byte[] RowData, ExpandingStorage<LineMemory> ParsedRecords, int PackageNumber,
    bool IsLastPackage = false)
{
    private static readonly ReadingPhasePackage _emptyPackage =
        new(Array.Empty<byte>(), new ExpandingStorage<LineMemory>(0), -1);

    public static ReadingPhasePackage Empty => _emptyPackage;
    public int PrePopulatedBytesLength { get; init; }
    public int ReadBytesLength { get; set; }
}

public record PreReadPackage(
    byte[] RemainedBytes,
    int RemainedBytesLength,
    int PackageNumber,
    bool IsLastPackage)
{
    public static PreReadPackage LastPackage(int packageNumber) => new PreReadPackage(Array.Empty<byte>(), 0, packageNumber, true);
};

public record SortingPhasePackage(
    byte[] RowData,
    int OccupiedLength,
    ExpandingStorage<LineMemory> ParsedRecords,
    int LinesNumber, 
    int PackageNumber,
    bool IsLastPackage);

public record AfterSortingPhasePackage(
    LineMemory[] SortedLines,
    byte[] RowData, 
    ExpandingStorage<LineMemory> ParsedRecords, 
    int LinesNumber, 
    int PackageNumber,
    bool IsLastPackage);