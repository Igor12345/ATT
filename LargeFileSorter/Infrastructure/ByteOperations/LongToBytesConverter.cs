using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Infrastructure.ByteOperations;

public class LongToBytesConverter : IDisposable, IAsyncDisposable
{
   //todo const or config
   private readonly byte[] _buffer = ArrayPool<byte>.Shared.Rent(21);

   //only for benchmarks
   public (ReadOnlyMemory<byte>, int length) ConvertLongToBytes(ulong value)
   {
      ReadOnlySpan<char> chars = value.ToString().AsSpan();

      for (int i = 0; i < chars.Length; i++)
      {
         _buffer[i] = (byte)chars[i];
      }

      return (_buffer.AsMemory(), chars.Length);
   }
   
   public static int WriteULongToBytes(ulong value, Span<byte> destination)
   {
      return ConvertULongToBytesInternal(value, destination);
   }

   //by motives System.Number
   private static unsafe int ConvertULongToBytesInternal(ulong value, Span<byte> destination)
   {
      int bufferLength = CountDigits(value);

      // string result = FastAllocateString(bufferLength);
      fixed (byte* buffer = destination)
      {
         byte* p = buffer + bufferLength;
         p = UInt64ToDecBytes(p, value);
         Debug.Assert(p == buffer);
      }

      return bufferLength;
   }

   public void Dispose()
   {
      ArrayPool<byte>.Shared.Return(_buffer);
   }

   public ValueTask DisposeAsync()
   {
      ArrayPool<byte>.Shared.Return(_buffer);
      return ValueTask.CompletedTask;
   }

   //by motives System.Number
#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
   internal static unsafe byte* UInt64ToDecBytes(byte* bufferEnd, ulong value)
   {
#if TARGET_32BIT
            while ((uint)(value >> 32) != 0)
            {
                bufferEnd = UInt32ToDecChars(bufferEnd, Int64DivMod1E9(ref value), 9);
            }
            return UInt32ToDecChars(bufferEnd, (uint)value);
#else
      do
      {
         ulong remainder;
         (value, remainder) = Math.DivRem(value, 10);
         *(--bufferEnd) = (byte)(remainder + '0');
      } while (value != 0);

      return bufferEnd;
#endif
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private static int CountDigits(ulong value)
   {
      int digits = 1;
      uint part;
      if (value >= 10000000)
      {
         if (value >= 100000000000000)
         {
            part = (uint)(value / 100000000000000);
            digits += 14;
         }
         else
         {
            part = (uint)(value / 10000000);
            digits += 7;
         }
      }
      else
      {
         part = (uint)value;
      }

      if (part < 10)
      {
         // no-op
      }
      else if (part < 100)
      {
         digits++;
      }
      else if (part < 1000)
      {
         digits += 2;
      }
      else if (part < 10000)
      {
         digits += 3;
      }
      else if (part < 100000)
      {
         digits += 4;
      }
      else if (part < 1000000)
      {
         digits += 5;
      }
      else
      {
         Debug.Assert(part < 10000000);
         digits += 6;
      }

      return digits;
   }
}