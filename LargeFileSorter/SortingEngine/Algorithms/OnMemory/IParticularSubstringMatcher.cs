namespace SortingEngine.Algorithms.OnMemory;

public interface IParticularSubstringMatcher
{
    int Find(ReadOnlyMemory<byte> text);
}