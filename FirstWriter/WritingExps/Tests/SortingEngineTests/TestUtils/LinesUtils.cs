using Infrastructure.ByteOperations;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.Utils;

namespace SortingEngineTests.TestUtils;

public class LinesUtils
{
    public static string LineToString(LineMemory line, byte[] source)
    {
        Span<byte> buffer = stackalloc byte[Constants.MaxLineLength_UTF8];
        int length = LineUtils.LineToBytes(line, source, buffer);
 
        return ByteToStringConverter.Convert(buffer[..length]);
    }
    
}