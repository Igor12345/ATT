using System.Text;

namespace ContentGenerator;

public class TextConvertor : ITextCreator
{
   private readonly ITextCreator _source;
   private readonly Encoding _encoding;

   public TextConvertor(ITextCreator source, Encoding encoding)
   {
      _source = source ?? throw new ArgumentNullException(nameof(source));
      _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
   }

   public IEnumerable<byte[]> GenerateBytes()
   {
      using var lines = _source.GenerateStrings().GetEnumerator();
      lines.MoveNext();
      while (true)
      {
         string str = lines.Current;
         yield return _encoding.GetBytes(str);
         lines.MoveNext();
      }
   }

   public IEnumerable<string> GenerateStrings()
   {
      return _source.GenerateStrings();
   }

   public Encoding Encoding => _encoding;
}