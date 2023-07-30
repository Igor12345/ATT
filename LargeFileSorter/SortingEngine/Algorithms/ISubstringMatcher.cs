namespace SortingEngine.Algorithms;

public interface ISubstringMatcher
{
    int Find(ReadOnlySpan<byte> text, byte[] pattern);
}