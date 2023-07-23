using System.Buffers;
using System.Text;
using Infrastructure.ByteOperations;
using SortingEngine;
using SortingEngine.Entities;
using Constants = Infrastructure.ByteOperations.Constants;

namespace SortingEngineTests.TestUtils;

public static class LinesUtils
{
    public static string LineToString(Line line, byte[] source, Encoding encoding)
    {
        Span<byte> buffer = stackalloc byte[TestConstants.MaxLineLengthUtf8];
        int length = LineToBytes(line, source, buffer, encoding);
 
        return ByteToStringConverter.Convert(buffer[..length], encoding);
    }

    public static int LineToBytes(Line line, ReadOnlySpan<byte> source, Span<byte> destination, Encoding encoding)
    {
        byte[]? rented = null;
        Span<byte> buffer = TestConstants.MaxLineLengthUtf8 <= Constants.MaxStackLimit
            ? stackalloc byte[TestConstants.MaxLineLengthUtf8]
            : rented = ArrayPool<byte>.Shared.Rent(TestConstants.MaxLineLengthUtf8);
        
        int length = LongToBytesConverter.WriteULongToBytes(line.Number, buffer, encoding);

        int fullLength = length + line.To - line.From;
        source[line.From..line.To].CopyTo(buffer[length..]);
        buffer.CopyTo(destination);
        if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);

        return fullLength;
    }
}