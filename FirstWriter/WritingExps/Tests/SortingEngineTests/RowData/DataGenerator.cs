using System.Text;
using Infrastructure.Parameters;

namespace SortingEngineTests.RowData;

public class DataGenerator
{
    private readonly Encoding _encoding;

    private static DataGenerator _default = new DataGenerator(Encoding.UTF8);
    public static DataGenerator UTF8 => _default;
    private DataGenerator(Encoding encoding)
    {
        _encoding = Guard.NotNull(encoding);
    }

    public static DataGenerator Use(Encoding encoding)
    {
        return new DataGenerator(encoding);
    }
    public byte[] Create(string[] lines, byte[] randomBytes)
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
        foreach (var line in lines)
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
}