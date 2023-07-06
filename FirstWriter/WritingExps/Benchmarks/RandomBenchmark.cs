using BenchmarkDotNet.Attributes;
using Benchmarks;

[MemoryDiagnoser()]
public class RandomBenchmark
{
   private readonly int _lines = 100_000;
   [Benchmark]
   public void TestFirst()
   {
      FirstFakeExecutor executor = new FirstFakeExecutor("", _lines);
      int res = executor.CreateFile();
   }
   [Benchmark]
   public void TestSecond()
   {
      SecondFakeExecutor executor = new SecondFakeExecutor("", _lines);
      int res = executor.CreateFile();
   }

   [Benchmark(Baseline = true)]
   public void TestThird()
   {
      ThirdFakeExecutor executor = new ThirdFakeExecutor("", _lines);
      int res = executor.CreateFile();
   }
}