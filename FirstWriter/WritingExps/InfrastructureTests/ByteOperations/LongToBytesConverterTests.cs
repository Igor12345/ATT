using System.Text;
using Infrastructure.ByteOperations;

namespace InfrastructureTests.ByteOperations;

public class LongToBytesConverterTests
{
    [Theory]
    [InlineData(123456)]
    [InlineData(Int64.MaxValue)]
    [InlineData(100000)]
    [InlineData(00010000)]
    public void LongToBytesConverter_ShouldConvertNumbersToBytes(ulong number)
    {
        string originValue = number.ToString();
        using LongToBytesConverter converter = new LongToBytesConverter();
        (ReadOnlyMemory<byte> buffer, int length) = converter.ConvertLongToBytes(number);

        string convertedValue = Encoding.UTF8.GetString(buffer.Span[..length]);
        
        Assert.Equal(convertedValue, originValue);
    }
    
    [Theory]
    [InlineData(123456)]
    [InlineData(Int64.MaxValue)]
    [InlineData(100000)]
    [InlineData(00010000)]
    public void LongToBytesConverter_ShouldConvertNumbersFillingBuffer(ulong number)
    {
        string originValue = number.ToString();
        Span<byte> buffer = stackalloc byte[20];

        int length = LongToBytesConverter.WriteULongToBytes(number, buffer);

        string convertedValue = Encoding.UTF8.GetString(buffer[..length]);
        
        Assert.Equal(convertedValue, originValue);
    }
}