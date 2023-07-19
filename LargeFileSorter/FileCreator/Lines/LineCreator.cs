using System.Text;
using FileCreator.Configuration;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;

namespace FileCreator.Lines;

public sealed class LineCreator
{
    //use ETW instead of logger
    private readonly ITextCreator _textCreator;
    private readonly IRuntimeConfiguration _config;
    private readonly byte[] _eol;
    private readonly byte[] _delimiter;
    private readonly int _delimiterLength;
    private readonly int _eolLength;
    private readonly Random _random;

    public LineCreator(IRuntimeConfiguration config, ITextCreator textCreator)
    {
        _config = Guard.NotNull(config);
        _textCreator = Guard.NotNull(textCreator);
        _random = _config.Seed > 0
            ? new Random(_config.Seed)
            : new Random();

        _eol = _config.Encoding.GetBytes(Environment.NewLine);
        _eolLength = _eol.Length;
        _delimiter = _config.Encoding.GetBytes(_config.Delimiter);
        _delimiterLength = _delimiter.Length;
    }

    //todo use memoization
    public int WriteLine(Memory<byte> lineBuffer)
    {
        PositionedBuffer destination = new PositionedBuffer(lineBuffer, 0);
        destination = WriteNumber(destination);
        destination = WriteDelimiter(destination);
        destination = WriteText(destination);
        destination = WriteEol(destination);

        return destination.Position;
    }

    private PositionedBuffer WriteNumber(PositionedBuffer destination)
    {
        //dirty hack, only half of numbers, if necessary it is possible to add abs of negative value to max
        ulong nextNumber = (ulong)_random.NextInt64(0, long.MaxValue);
        int position;
        if (Equals(_config.Encoding, Encoding.UTF8) || Equals(_config.Encoding, Encoding.ASCII))
        {
            position = LongToBytesConverter.WriteULongToBytes(nextNumber, destination.Buffer.Span);
        }
        else
        {
            position = _config.Encoding.GetBytes(nextNumber.ToString(), destination.Buffer.Span);
        }

        return destination with{Position = position};
    }

    private PositionedBuffer WriteDelimiter(PositionedBuffer destination)
    {
        _delimiter.CopyTo(destination.Buffer.Span[destination.Position..]);
        return destination with{Position = destination.Position + _delimiterLength};
    }

    private PositionedBuffer WriteText(PositionedBuffer destination)
    {
        return _textCreator.WriteText(destination);
    }

    private PositionedBuffer WriteEol(PositionedBuffer destination)
    {
        _eol.CopyTo(destination.Buffer[destination.Position..]);
        return destination with{Position = destination.Position+_eolLength};
    }
}
