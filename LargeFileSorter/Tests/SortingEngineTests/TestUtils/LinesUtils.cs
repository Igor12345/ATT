﻿using System.Buffers;
using Infrastructure.ByteOperations;
using SortingEngine;
using SortingEngine.Entities;

namespace SortingEngineTests.TestUtils;

public static class LinesUtils
{
    public static string LineToString(Line line, byte[] source)
    {
        Span<byte> buffer = stackalloc byte[TestConstants.MaxLineLengthUtf8];
        int length = LineToBytes(line, source, buffer);
 
        return ByteToStringConverter.Convert(buffer[..length]);
    }

    public static int LineToBytes(Line line, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        byte[]? rented = null;
        Span<byte> buffer = TestConstants.MaxLineLengthUtf8 <= Constants.MaxStackLimit
            ? stackalloc byte[TestConstants.MaxLineLengthUtf8]
            : rented = ArrayPool<byte>.Shared.Rent(TestConstants.MaxLineLengthUtf8);
        
        int length = LongToBytesConverter.WriteULongToBytes(line.Number, buffer);

        int fullLength = length + line.To - line.From;
        source[line.From..line.To].CopyTo(buffer[length..]);
        buffer.CopyTo(destination);
        if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);

        return fullLength;
    }
}