using Infrastructure.Parameters;

namespace FileCreator.Lines;

public sealed class LinesGenerator
{
    private readonly LineCreator _lineCreator;

    public LinesGenerator(LineCreator lineCreator)
    {
        _lineCreator = Guard.NotNull(lineCreator);
    }

    public IEnumerable<int> Generate(Memory<byte> buffer)
    {
        while (true)
        {
            int lineLength = _lineCreator.WriteLine(buffer);
            yield return lineLength;
        }
    }
}