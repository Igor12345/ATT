using System.Text;
using Infrastructure.Parameters;
using SortingEngine;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SortingEngineTests.TestUtils;

public class DataGenerator
{
    private readonly Encoding _encoding;

    private static readonly DataGenerator _default = new(Encoding.UTF8);
    private readonly byte[] _eol;
    private readonly byte[] _delimiter;
    public static DataGenerator UTF8 => _default;
    private DataGenerator(Encoding encoding)
    {
        _encoding = Guard.NotNull(encoding);
        _eol = encoding.GetBytes(Environment.NewLine);
        _delimiter = encoding.GetBytes(Constants.Delimiter);
    }

    public static DataGenerator Use(Encoding encoding)
    {
        return new DataGenerator(encoding);
    }
    public byte[] CreateWholeBytes(string[] lines, byte[] randomBytes)
    {
        int fullLength = 0;
        foreach (string line in lines)
        {
            fullLength += _encoding.GetByteCount(line);
        }

        fullLength += randomBytes.Length;
        fullLength += lines.Length * _encoding.GetBytes(Environment.NewLine).Length;
        Span<byte> buffer = new byte[fullLength];

        int currentPosition = 0;
        foreach (string line in lines)
        {
            int length = _encoding.GetBytes(line, buffer[currentPosition..]);
            currentPosition += length;
            length = _encoding.GetBytes(Environment.NewLine, buffer[currentPosition..]);
            currentPosition += length;
        }
        randomBytes.CopyTo(buffer[currentPosition..]);
        return buffer.ToArray();
    }

    public static byte[] RandomBytes(int length)
    {
        Random random = Random.Shared;
        byte[] result = new byte[length];
        random.NextBytes(result);
        return result;
    }

    public (LineMemory[], byte[]) CreateLinesFromStrings(string[] originalStrings)
    {
        byte[] source = CreateWholeBytes(originalStrings, RandomBytes(242));
        RecordsExtractor extractor = new RecordsExtractor(_eol, _delimiter);
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(100);
        ExtractionResult result = extractor.ExtractRecords(source.AsSpan(), linesStorage);

        LineMemory[] lines = new LineMemory[result.LinesNumber];
        for (int i = 0; i < result.LinesNumber; i++)
        {
            lines[i] = linesStorage[i];
        }

        return (lines, source);
    }

    public byte[] FillLinesStorageFromStrings(string[] originalStrings, ExpandingStorage<LineMemory> linesStorage)
    {
        byte[] source = CreateWholeBytes(originalStrings, RandomBytes(242));
        RecordsExtractor extractor = new RecordsExtractor(_eol, _delimiter);
        ExtractionResult result = extractor.ExtractRecords(source.AsSpan(), linesStorage);

        LineMemory[] lines = new LineMemory[result.LinesNumber];
        for (int i = 0; i < result.LinesNumber; i++)
        {
            lines[i] = linesStorage[i];
        }

        return source;
    }
}