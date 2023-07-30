namespace SortingEngine.Algorithms;

public interface IParticularSubstringMatcher
{
    int Find(ReadOnlySpan<byte> text);
}