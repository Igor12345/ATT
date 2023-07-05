using ContentGenerator;

namespace SimpleWriter
{
   internal class FileCreator
   {
      private readonly ITextCreator _textCreator;
      public FileCreator(ITextCreator textCreator)
      {
         _textCreator = textCreator ?? throw new ArgumentNullException(nameof(textCreator));
      }

      public async Task CreateBinaryFileAsync(string fileName, int linesNumber)
      {
         await using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
         await using BinaryWriter writer = new BinaryWriter(fileStream, _textCreator.Encoding);
         using IEnumerator<byte[]> bytesSource = _textCreator.GenerateBytes().GetEnumerator();

         byte[] newLine = _textCreator.Encoding.GetBytes(Environment.NewLine);
         int i = 0;

         foreach (byte[] bytes in _textCreator.GenerateBytes())
         {
            if (++i >= linesNumber)
               break;
            await writer.BaseStream.WriteAsync(bytes);
            await writer.BaseStream.WriteAsync(newLine);
         }
      }

      public async Task CreateStringFileAsync(string fileName, int linesNumber)
      {
         await using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
         await using TextWriter writer = new StreamWriter(fileStream); 

         int i = 0;
         foreach (string line in _textCreator.GenerateStrings())
         {
            if (++i >= linesNumber)
               break;
            await writer.WriteLineAsync(line);
            // await writer.WriteLineAsync(Environment.NewLine);
         }
      }
   }
}
