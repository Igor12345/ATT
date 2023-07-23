using System.Buffers;
using System.Text;

namespace Infrastructure.ByteOperations;

public class ByteToStringConverter
{
   public static string Convert(ReadOnlySpan<byte> lineSpan)
   {
      return new string(lineSpan.ToArray().Select(b => (char)b).ToArray());
   }
   
   public static string Convert(ReadOnlySpan<byte> lineSpan, Encoding encoding)
   {
      int charsLength = encoding.GetCharCount(lineSpan);
      char[]? rented = null;
      Span<char> chars = charsLength < Constants.MaxStackLimit
         ? stackalloc char[charsLength]
         : rented = ArrayPool<char>.Shared.Rent(charsLength);
      try
      {
         encoding.GetChars(lineSpan, chars);
         return new string(chars);
      }
      finally
      {
         if(rented!=null)
            ArrayPool<char>.Shared.Return(rented);
      }
   }
}