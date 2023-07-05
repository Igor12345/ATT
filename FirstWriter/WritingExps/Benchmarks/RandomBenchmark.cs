using BenchmarkDotNet.Attributes;
using Benchmarks;

[MemoryDiagnoser()]
public class RandomBenchmark
{
   [Benchmark]
   public void TestFirst()
   {
      FirstFakeExecutor executor = new FirstFakeExecutor("", 10000);
      int res = executor.CreateFile();
   }
   [Benchmark]
   public void TestSecond()
   {
      SecondFakeExecutor executor = new SecondFakeExecutor("", 10000);
      int res = executor.CreateFile();
   }

   [Benchmark(Baseline = true)]
   public void TestThird()
   {
      ThirdFakeExecutor executor = new ThirdFakeExecutor("", 10000);
      int res = executor.CreateFile();
   }
}