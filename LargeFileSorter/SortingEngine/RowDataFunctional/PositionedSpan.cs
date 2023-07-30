namespace SortingEngine.RowDataFunctional;

public struct PositionedSpan<T>
{
    //see Microsoft.Toolkit.HighPerformance.Enumerables.SpanEnumerable<T>
    public PositionedSpan(ReadOnlyMemory<T> buffer, int start, int length)
    {
        Buffer = buffer;
        Start = start;
        Length = length;
    }

    public ReadOnlyMemory<T> Buffer { get; }
    public int Start { get; }
    public int Length { get; }
}