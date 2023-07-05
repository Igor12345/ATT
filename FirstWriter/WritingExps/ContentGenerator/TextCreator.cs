using System.Text;

namespace ContentGenerator
{
   public class TextCreator : ITextCreator
   {
      private readonly int _maxStringLength;
      public int MaxByte { get; init; }
      public int MinByte { get; init; }
      private readonly bool _memoryMode;
      private readonly Encoding _encoding;
      private readonly List<string> _created = new List<string>();

      //todo what's new
      private readonly Random _random = new();

      public static ITextCreator NumbersCreator => new TextCreator(19, Encoding.ASCII, false)
         { MinByte = 48, MaxByte = 57 };

      public TextCreator(int maxStringLength = 1024, Encoding? encoding = null, bool memoryMode = false)
      {
         _maxStringLength = maxStringLength;
         _memoryMode = memoryMode;
         _encoding = encoding ?? Encoding.ASCII;
         MinByte = 32;
         MaxByte = 126;
      }

      public Encoding Encoding => _encoding;

      public IEnumerable<string> GenerateStrings()
      {
         if (_memoryMode && _created.Any())
         {
            foreach (string s in _created)
            {
               yield return s;
            }

            yield break;
         }

         while (true)
         {
            int length = _random.Next(1, _maxStringLength);
            byte[] bytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
               bytes[i] = (byte)_random.Next(MinByte, MaxByte);
            }

            string str = _encoding.GetString(bytes);
            if (_memoryMode)
               _created.Add(str);
            yield return str;
         }
      }
      public IEnumerable<byte[]> GenerateBytes()
      {
         while (true)
         {
            int length = _random.Next(1, _maxStringLength);
            byte[] bytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
               bytes[i] = (byte)_random.Next(MinByte, MaxByte);
            }

            yield return bytes;
         }
      }

   }
}