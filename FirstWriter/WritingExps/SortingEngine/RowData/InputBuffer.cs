using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class InputBuffer
{
    public ReadOnlyMemory<byte> Write { get; set; }
    public byte[] Buffer { get; set; }
    public int UsedLength { get; set; }
    
    public int ReadBytes { get; set; }
}
public class PreSortBuffer
{
    public ReadOnlyMemory<byte> Write { get; set; }
    public ExpandingStorage<LineMemory> RecordsStorage { get; set; }
    public byte[] Buffer { get; set; }
    public int LinesNumber { get; set; }
    public int ReadBytes { get; set; }
}