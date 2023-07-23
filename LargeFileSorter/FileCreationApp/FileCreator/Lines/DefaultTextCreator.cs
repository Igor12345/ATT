using FileCreator.Configuration;
using Infrastructure.Parameters;

namespace FileCreator.Lines;

public class DefaultTextCreator : ITextCreator
{
    private readonly IRuntimeConfiguration _config;
    private readonly Random _random;
    private readonly char[] _charactersPool;
    private readonly Dictionary<char, byte[]> _characterBytes;
    
    private readonly int _charactersLength;

    public DefaultTextCreator(IRuntimeConfiguration config)
    {
        _config = Guard.NotNull(config);
        _random = _config.Seed > 0
            ? new Random(_config.Seed)
            : new Random();

        _charactersPool = _config.PossibleCharacters.ToArray();
        _characterBytes = _charactersPool.ToDictionary(c => c, c => _config.Encoding.GetBytes(new[] { c }));
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

        int j = 0;
        for (int i = 0; i < length; i++)
        {
            var letter = _charactersPool[_random.Next(0, _charactersLength - 1)];
            byte[] bytes = _characterBytes[letter];
            foreach (byte b in bytes)
            {
                buffer[j++] = b;
            }
        }

        return length;
    }
}