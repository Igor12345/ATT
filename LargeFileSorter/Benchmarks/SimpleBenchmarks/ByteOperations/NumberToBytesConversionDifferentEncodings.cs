using System.Text;
using BenchmarkDotNet.Attributes;
using Infrastructure.ByteOperations;

namespace SimpleBenchmarks.ByteOperations;

[MemoryDiagnoser]
[Config(typeof(InProcessNoEmitConfig))]
public class NumberToBytesConversionDifferentEncodings
{
    [Params(100, 1_000, 10_000)] public int N;

    private ulong[]? Numbers;

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
    // [Benchmark]
    public int ConvertNumbersUsingUtf8Hack()
    {
        int s = 0;

        int maxNumberLength = 21;
        Span<byte> buffer = stackalloc byte[maxNumberLength];
        for (int i = 0; i < N; i++)
        {
            int length = LongToBytesConverter.WriteULongToBytes(Numbers[i], buffer);
            s += length - buffer.Length;
        }

        return s;
    }

    [GcServer(true)]
    [Benchmark]
    public int ConvertNumbersToUtf32ThroughUts8()
    {
        int s = 0;

        int maxNumberLength = 21;
        Span<byte> buffer = stackalloc byte[maxNumberLength * 4];
        Encoding encoding = Encoding.UTF8;
        for (int i = 0; i < N; i++)
        {
            int length = LongToBytesConverter.WriteULongToBytes(Numbers[i], buffer, encoding);
            s += length - buffer.Length;
        }

        return s;
    }

    [GcServer(true)]
    // [Benchmark]
    public int ConvertNumbersToUtf32Standard()
    {
        int s = 0;

        int maxNumberLength = 21;
        Span<byte> buffer = stackalloc byte[maxNumberLength * 4];
        for (int i = 0; i < N; i++)
        {
            int length = LongToBytesConverter.WriteULongToBytesStandard(Numbers[i], buffer, Encoding.UTF32);
            s += length - buffer.Length;
        }

        return s;
    }
}