using Infrastructure.ByteOperations;
using SortingEngine.Entities;

namespace SortingEngine.Utils;

public class LineUtils
{
    public static int LineToBytes(LineMemory line, ReadOnlySpan<byte> source, Span<byte> destination,
        LongToBytesConverter longToBytesConverter)
    {
        var (numberBytes, length) = longToBytesConverter.ConvertLongToBytes(line.Number);

        int fullLength = length + line.To - line.From;
        Span<byte> buffer = stackalloc byte[fullLength];
        numberBytes.Span[..length].CopyTo(buffer[..length]);
        source[line.From..line.To].CopyTo(buffer[length..]);
        buffer.CopyTo(destination);
        return fullLength;
    }

    public static int LineToBytes(LineMemory line, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        //todo
        //for UTF8 4 = 2+2 = eol+delimiter
        Span<byte> buffer = stackalloc byte[Constants.MaxLineLength_UTF8];
        var length = LongToBytesConverter.WriteULongToBytes(line.Number, buffer);

        int fullLength = length + line.To - line.From;
        source[line.From..line.To].CopyTo(buffer[length..]);
        buffer.CopyTo(destination);
        return fullLength;
    }
}