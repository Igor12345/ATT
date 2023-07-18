using System.Text;
using FileCreator.Configuration;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using Microsoft.Extensions.Logging;

namespace FileCreator.Lines;

public sealed class LineCreator
{
    //use ETW instead of it
    private readonly ILogger _logger;
    private readonly IRuntimeConfiguration _config;
    private readonly Dictionary<ulong, int> _duplicates = new ();
    private ulong _counter;
    private readonly Random _random;
    private readonly byte[][] _samples;
    private readonly byte[] _eol;
    private readonly byte[] _delimiter;
    private readonly int _delimiterLength;
    private readonly int _eolLength;
    private readonly byte[] _charactersPool;
    private readonly int _charactersLength;

    public LineCreator(IRuntimeConfiguration config, ILogger logger)
    {
        _config = Guard.NotNull(config);
        _logger = Guard.NotNull(logger);
        _samples = new byte[_config.Samples.Length][];
        _random = _config.Seed > 0 
            ? new Random(_config.Seed) 
            : new Random();

        for (int i = 0; i < _config.Samples.Length; i++)
        {
            _samples[i] = _config.Encoding.GetBytes(_config.Samples[i]);
        }

        _eol = _config.Encoding.GetBytes(Environment.NewLine);
        _eolLength = _eol.Length;
        _delimiter = _config.Encoding.GetBytes(_config.Delimiter);
        _delimiterLength = _delimiter.Length;

        _charactersPool = _config.Encoding.GetBytes(_config.PossibleCharacters);
        _charactersLength = _charactersPool.Length;
    }

    //todo use memoization
    public int WriteLine(Memory<byte> lineBuffer)
    {
        //dirty hack, only half of numbers, if necessary it is possible to add abs of negative value to max
        ulong nextNumber = (ulong)_random.NextInt64(0, long.MaxValue);
        int startTextPosition;
        if (Equals(_config.Encoding, Encoding.UTF8) || Equals(_config.Encoding, Encoding.ASCII))
        {
            startTextPosition = LongToBytesConverter.WriteULongToBytes(nextNumber, lineBuffer.Span);
        }
        else
        {
            startTextPosition = _config.Encoding.GetBytes(nextNumber.ToString(), lineBuffer.Span);
        }

        int fullLength;
        byte[] nextDuplicate = TimeForDuplicate();

        _delimiter.CopyTo(lineBuffer.Span[startTextPosition..]);
        startTextPosition += _delimiterLength;
        
        if (nextDuplicate.Length == 0)
        {
            fullLength = GenerateNextText(lineBuffer.Span[startTextPosition..]);
        }
        else
        {
            nextDuplicate.CopyTo(lineBuffer[startTextPosition..]);
            fullLength = startTextPosition + nextDuplicate.Length;
        }
        _eol.CopyTo(lineBuffer[fullLength..]);
        fullLength += _eolLength;

        _counter++;
        return fullLength;
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

    //init first time
    private byte[] TimeForDuplicate()
    {
        if (_config.Samples.Length == 0 || !_duplicates.ContainsKey(_counter))
            return Array.Empty<byte>();


        int index = _duplicates[_counter];
        var text = _samples[index];
        ulong nextTime = (ulong)_random.Next(1, _config.DuplicationFrequency) + _counter;

        if (_duplicates.ContainsKey(nextTime))
        {
            //looking for next empty slot, there should be at least one, because Samples.Length < DuplicationFrequency
            for (int i = 1; i <= _config.DuplicationFrequency; i++)
            {
                if (_duplicates.ContainsKey((ulong)i + _counter)) continue;
                nextTime = (ulong)i + _counter;
                break;
            }
        }
        
        _duplicates.Remove(_counter);
        _duplicates.Add(nextTime, index);
        return text;
    }
}