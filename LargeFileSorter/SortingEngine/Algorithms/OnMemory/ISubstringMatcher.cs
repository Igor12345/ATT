namespace SortingEngine.Algorithms.OnMemory;

public interface ISubstringMatcher
{
    int Find(ReadOnlyMemory<byte> text, byte[] pattern);
}