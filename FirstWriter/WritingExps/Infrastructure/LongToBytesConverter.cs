using System.Buffers;

namespace Infrastructure;

public class LongToBytesConverter : IDisposable, IAsyncDisposable
{
   private readonly byte[] _buffer;

   public LongToBytesConverter()
   {
      _buffer = ArrayPool<byte>.Shared.Rent(20);
   }

   public (ReadOnlyMemory<byte>, int length) ConvertLongToBytes(long value)
   {
      ReadOnlySpan<char> chars = value.ToString().AsSpan();

      for (int i = 0; i < chars.Length; i++)
      {
         _buffer[i] = (byte)chars[i];
      }

      return (_buffer.AsMemory(), chars.Length);
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
}