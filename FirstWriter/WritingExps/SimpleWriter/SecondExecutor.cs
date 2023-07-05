using System.Text;
using ContentGenerator;

namespace SimpleWriter;

internal class SecondExecutor
{
   private readonly string _fileName;
   private readonly long _lines;

   public SecondExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
   }

   public async Task CreateFile()
   {
      LinesCreator linesCreator = new LinesCreator();
      byte[] line = new byte[19 + 4 + 1024];

      string fileName = @$"d://temp/ATT/{_fileName}.txt";
      await using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
      await using BinaryWriter writer = new BinaryWriter(fileStream, Encoding.UTF8);
         
      for (int i = 0; i < _lines; i++)
      {
         int length = linesCreator.NextLine(line);
         await writer.BaseStream.WriteAsync(line, 0, length);
      }
   }
}