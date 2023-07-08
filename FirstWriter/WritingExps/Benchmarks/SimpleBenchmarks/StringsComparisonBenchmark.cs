
using BenchmarkDotNet.Attributes;
using System.Text;
using SimpleReader;
using SortingEngine.Comparators;

[MemoryDiagnoser()]
public class StringsComparisonBenchmark
{
   private Random _random = null!;
   private Encoding _encoding = Encoding.UTF8;
   private int _maxStringLength = 50;

   [Params(100, 1000, 10000)]
   public int N;

   private string[] _stringLines;
   private byte[][] _byteLines;

   [GlobalSetup]
   public void GlobalSetup()
   {
      _random = new Random(42);
      _stringLines = new string[N]; 
      _byteLines = new byte[N][];
      for (int i = 0; i < N; i++)
      {
         (_byteLines[i], _stringLines[i]) = GenerateRandom(_maxStringLength);
      }
   }

   private (byte[], string) GenerateRandom(int n)
   {
      int length = _random.Next(1, n);
      byte[] result = new byte[length];

      int minByte = 32;
      int maxByte = 126;
      for (int i = 0; i < length; i++)
      {
         result[i] = (byte)_random.Next(minByte, maxByte);
      }
      // string str = _encoding.GetString(result);
      return (result, "");
   }

   // [Benchmark(Baseline = true)]
   public int CompareStrings()
   {
      int result = 0;
      for (int i = 0; i < N-1; i++)
      {
         result += string.CompareOrdinal(_stringLines[i], _stringLines[i + 1]);
      }
      return result;
   }

   [Benchmark(Baseline = true)]
   public int CompareStringsRunTimeCreation()
   {
      int result = 0;
      for (int i = 0; i < N - 1; i++)
      {
         result += string.CompareOrdinal(_encoding.GetString(_byteLines[i]), _encoding.GetString(_byteLines[i + 1]));
      }
      return result;
   }

   [Benchmark]
   public int CompareAsBytes()
   {
      int result = 0;
      for (int i = 0; i < N - 1; i++)
      {
         result += StringAsBytesComparer.Compare(_byteLines[i], _byteLines[i + 1]);
      }
      return result;
   }
}