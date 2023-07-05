using ContentGenerator;

namespace SimpleWriter;

internal class ThirdExecutor
{
   private readonly string _fileName;
   private readonly long _lines;
   private readonly int _maxLineLength;
   private readonly int _chankSize = 10_000;

   public ThirdExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
      _maxLineLength = 19 + 4 + 1024;
   }

   public async Task CreateFile()
   {
      LinesCreator linesCreator = new LinesCreator();
      byte[] line = new byte[_maxLineLength*_chankSize];

      string fileName = @$"d://temp/ATT/{_fileName}.txt";
      await using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
      int start = 0;
      int length;
      for (int i = 0; i < _lines; i++)
      {
         length = linesCreator.NextLine(line, start);
         start += length;
         if (_maxLineLength * _chankSize - start < _maxLineLength)
         {
            await fileStream.WriteAsync(line, 0, start);
            start = 0;
         }
      }
      await fileStream.WriteAsync(line, 0, start);
   }
}