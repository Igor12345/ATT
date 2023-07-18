using FileCreator.Configuration;
using Infrastructure.Parameters;

namespace FileCreator.Lines;

public class DuplicatesTextCreator : ITextCreator
{
    private readonly ITextCreator _textCreator;
    private readonly IRuntimeConfiguration _config;
    private readonly Dictionary<ulong, int> _duplicates = new();
    private readonly Random _random;
    private readonly byte[][] _samples;
    private ulong _counter;

    public DuplicatesTextCreator(IRuntimeConfiguration config, ITextCreator textCreator)
    {
        _config = Guard.NotNull(config);
        _textCreator = Guard.NotNull(textCreator);
        _samples = new byte[_config.Samples.Length][];
        _random = _config.Seed > 0
            ? new Random(_config.Seed)
            : new Random();

        for (int i = 0; i < _config.Samples.Length; i++)
        {
            _samples[i] = _config.Encoding.GetBytes(_config.Samples[i]);
            ulong nextTime = TakeNextEmptySlotForRepetition();
            _duplicates.Add(nextTime, i);
        }
    }

    public PositionedBuffer WriteText(PositionedBuffer destination)
    {
        _counter++;
        byte[] nextDuplicate = TimeForDuplicate();

        if (nextDuplicate.Length == 0)
        {
            return _textCreator.WriteText(destination);
        }

        nextDuplicate.CopyTo(destination.Buffer[destination.Position..]);
        return destination with { Position = destination.Position + nextDuplicate.Length };
    }

    private ulong TakeNextEmptySlotForRepetition()
    {
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

        return nextTime;
    }

    private byte[] TimeForDuplicate()
    {
        if (_config.Samples.Length == 0 || !_duplicates.ContainsKey(_counter))
            return Array.Empty<byte>();

        int index = _duplicates[_counter];
        byte[] text = _samples[index];
        ulong nextTime = TakeNextEmptySlotForRepetition();

        _duplicates.Remove(_counter);
        _duplicates.Add(nextTime, index);
        return text;
    }
}