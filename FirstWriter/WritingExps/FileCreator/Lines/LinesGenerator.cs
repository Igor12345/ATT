using Infrastructure.Parameters;

namespace FileCreator.Lines;


//It could be better to make this class responsible for the repetition
//of the text parts of lines, but that would have required it
//to know about the internal structure of a line. 
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