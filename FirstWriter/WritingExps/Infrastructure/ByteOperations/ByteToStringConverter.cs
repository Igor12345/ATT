namespace Infrastructure.ByteOperations;

public class ByteToStringConverter
{
   public static string Convert(ReadOnlySpan<byte> lineSpan)
   {
      return new string(lineSpan.ToArray().Select(b => (char)b).ToArray());
   }

   public static string Convert(byte[] lineSpan)
   {
      return new string(lineSpan.ToArray().Select(b => (char)b).ToArray());
   }

   public static string Convert(Memory<byte> lineSpan)
   {
      return new string(lineSpan.ToArray().Select(b => (char)b).ToArray());
   }
}