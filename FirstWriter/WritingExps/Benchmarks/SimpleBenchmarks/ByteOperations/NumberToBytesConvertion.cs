using BenchmarkDotNet.Attributes;
using Infrastructure.ByteOperations;

namespace SimpleBenchmarks.ByteOperations;

[MemoryDiagnoser]
public class NumberToBytesConversion
{
    [Params(100, 1_000, 10_000)]
    public int N;
    
    private ulong[] Numbers;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        Numbers = new ulong[N];
        for (int i = 0; i < N; i++)
        {
            Numbers[i] = (ulong)Random.Shared.NextInt64(0, Int64.MaxValue);
        }
    }

    [GcServer(true)]
    [Benchmark]
    public int ConvertNumbersAcquiringArray()
    {
        int s = 0;
        using LongToBytesConverter converter = new LongToBytesConverter();
        for (int i = 0; i < N; i++)
        {
            var (buffer, length) = converter.ConvertLongToBytes(Numbers[i]);
            s += length - buffer.Length;
        }
        return s;
    }

    [GcServer(true)]
    [Benchmark]
    public int ConvertNumbersFillingBuffer()
    {
        int s = 0;

        Span<byte> buffer = stackalloc byte[20];
        for (int i = 0; i < N; i++)
        { 
            int length = LongToBytesConverter.WriteULongToBytes(Numbers[i], buffer);
            s += length - buffer.Length;
        }
        return s;
    }
}
