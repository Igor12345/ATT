using System.Text;

namespace ContentGenerator
{
   public class TextCreator:ITextCreator
   {
      private readonly int _maxLength;
      private readonly bool _memoryMode;
      private readonly Encoding _encoding;
      private readonly List<string> _created = new List<string>();

      //todo what's new
      private readonly Random _random = new();

      public TextCreator(int maxLength = 30, Encoding? encoding = null, bool memoryMode = false)
      {
         _maxLength = maxLength;
         _memoryMode = memoryMode;
         _encoding = encoding ?? new UTF8Encoding();
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
            int length = _random.Next(0, _maxLength);
            byte[] bytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
               bytes[i] = (byte)_random.Next(65, 122);
            }

            string str = _encoding.GetString(bytes);
            if(_memoryMode)
               _created.Add(str);
            yield return str;
         }
      }

      public IEnumerable<byte[]> GenerateBytes()
      {
         while (true)
         {
            int length = _random.Next(0, _maxLength);
            byte[] bytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
               bytes[i] = (byte)_random.Next(65, 122);
            }

            yield return bytes;
         }
      }

   }
}