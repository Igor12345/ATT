using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBenchmarks
{
   [MemoryDiagnoser]
   public class ArrayVsSpan
   {
      private Random _random = null!;
      [Params(100, 1000, 10000)]
      public int N;
      
      private byte[] _bytes;

      [GlobalSetup]
      public void GlobalSetup()
      {
         _random = new Random(42);
         _bytes = new byte[N];
         for (int i = 0; i < N; i++)
         {
            _bytes[i]= (byte)_random.Next(1, 127);
         }
      }

      [Benchmark]
      public int UseArraySlice()
      {
         int result = 0;

         for (int i = 0; i < N; i++)
         {
            result += SumSlice(_bytes[..i]);
         }

         return result;
      }

      [Benchmark]
      public int UseSpanWithAllocation()
      {
         int result = 0;

         for (int i = 0; i < N; i++)
         {
            result += SumSpan(_bytes[..i]);
         }

         return result;
      }

      [Benchmark]
      public int UseSpanNoAllocation()
      {
         int result = 0;
         var span = _bytes.AsSpan();

         for (int i = 0; i < N; i++)
         {
            result += SumSpan(span[..i]);
         }

         return result;
      }

      [Benchmark]
      public int UseMemoryNoAllocation()
      {
         int result = 0;
         var span = _bytes.AsMemory();

         for (int i = 0; i < N; i++)
         {
            result += SumMemory(span[..i]);
         }

         return result;
      }

      private int SumSlice(byte[] slice)
      {
         int result = 0;
         for (int i = 0; i < slice.Length; i++)
         {
            result += slice[i];
         }

         return result;
      }
      private int SumSpan(Span<byte> span)
      {
         int result = 0;
         for (int i = 0; i < span.Length; i++)
         {
            result += span[i];
         }

         return result;
      }
      private int SumMemory(ReadOnlyMemory<byte> memory)
      {
         int result = 0;
         var span = memory.Span;
         for (int i = 0; i < span.Length; i++)
         {
            result += span[i];
         }

         return result;
      }
   }
}
