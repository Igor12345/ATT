using Infrastructure.ByteOperations;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace InfrastructureTests
{
   public class UnitTest1
   {
      [Fact]
      public void LongToBytes()
      {
         Random random = new Random();
         ulong example = (ulong)random.NextInt64(0, Int64.MaxValue);
         string str = example.ToString();
         string str2 = UInt64ToDecStr(example);
         byte[] bytes = UInt64ToDecBytes(example);
         Span<byte> span = stackalloc byte[20];
         Span<byte> span2 = stackalloc byte[20];
         int length = UInt64ToDecBytes(example, span);
         int length2 = LongToBytesConverter.WriteULongToBytes(example, span2);

         byte[] bytesFromStr = str.Select(c => (byte)c).ToArray();

         Span<byte> buffer = stackalloc byte[20];

         bool success = BitConverter.TryWriteBytes(buffer, example);

         byte[] fromConverter = buffer.ToArray();

         for (int i = 0; i < bytesFromStr.Length; i++)
         {
            Assert.Equal(bytesFromStr[i], fromConverter[i]);
         }
      }

      internal static unsafe string UInt64ToDecStr(ulong value)
      {
         // Intrinsified in mono interpreter
         int bufferLength = CountDigits(value);

         char[] chars = new char[bufferLength];
         // string result = FastAllocateString(bufferLength);
         fixed (char* buffer = chars)
         {
            char* p = buffer + bufferLength;
            p = UInt64ToDecChars(p, value);
            Debug.Assert(p == buffer);
         }
         return new string(chars);
      }

      internal static unsafe byte[] UInt64ToDecBytes(ulong value)
      {
         // Intrinsified in mono interpreter
         int bufferLength = CountDigits(value);

         byte[] bytes = new byte[bufferLength];
         // string result = FastAllocateString(bufferLength);
         fixed (byte* buffer = bytes)
         {
            byte* p = buffer + bufferLength;
            p = UInt64ToDecBytes(p, value);
            Debug.Assert(p == buffer);
         }
         return bytes;
      }

      internal static unsafe int UInt64ToDecBytes(ulong value, Span<byte> destination)
      {
         // Intrinsified in mono interpreter
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

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int CountDigits(ulong value)
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

      //System.Number 
#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
      internal static unsafe char* UInt64ToDecChars(char* bufferEnd, ulong value)
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
            *(--bufferEnd) = (char)(remainder + '0');
         }
         while (value != 0);

         return bufferEnd;
#endif
      }

      //System.Number 
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
         }
         while (value != 0);

         return bufferEnd;
#endif
      }
   }

}