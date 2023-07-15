namespace SortingEngine.Entities;

public readonly record struct LineRecord(ulong Number, byte[] Text);
public readonly record struct LineAsString(ulong Number, string Text);

public readonly record struct LineMemory(ulong Number, int From, int To)
{
   //todo
   public int WriteBytes(Memory<byte> buffer, ReadOnlyMemory<byte> source)
   {
      ReadOnlySpan<char> chars = Number.ToString().AsSpan();
      var bytes = BitConverter.TryWriteBytes(buffer.Span, Number);
      var span = buffer.Span;
      for (int i = 0; i < chars.Length; i++)
      {
         span[i] = (byte)chars[i];
      }
      source[From..To].CopyTo(buffer[chars.Length..]);
      return chars.Length + (To - From);
   }
}