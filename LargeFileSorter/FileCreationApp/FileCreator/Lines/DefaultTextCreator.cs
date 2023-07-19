using FileCreator.Configuration;
using Infrastructure.Parameters;

namespace FileCreator.Lines;

public class DefaultTextCreator : ITextCreator
{
    private readonly IRuntimeConfiguration _config;
    private readonly Random _random;
    private readonly byte[] _charactersPool;
    private readonly int _charactersLength;

    public DefaultTextCreator(IRuntimeConfiguration config)
    {
        _config = Guard.NotNull(config);
        _random = _config.Seed > 0
            ? new Random(_config.Seed)
            : new Random();

        _charactersPool = _config.Encoding.GetBytes(_config.PossibleCharacters);
        _charactersLength = _charactersPool.Length;
    }

    public PositionedBuffer WriteText(PositionedBuffer destination)
    {
        int length = GenerateNextText(destination.Buffer.Span[destination.Position..]);
        return destination with { Position = destination.Position + length };
    }

    private int GenerateNextText(Span<byte> buffer)
    {
        int length = _random.Next(1, _config.MaxTextLength);

        for (int i = 0; i < length; i++)
        {
            buffer[i] = _charactersPool[_random.Next(0, _charactersLength - 1)];
        }

        return length;
    }
}