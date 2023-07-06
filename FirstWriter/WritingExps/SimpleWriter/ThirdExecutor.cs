using ContentGenerator;

namespace SimpleWriter;

internal class ThirdExecutor
{
   private readonly string _fileName;
   private readonly long _lines;
   private readonly int _maxLineLength;
   private readonly int _chunkSize = 15;

   public ThirdExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
      _maxLineLength = ConstValues.MaxNumberLength + 4 + ConstValues.MaxTextLength;
   }

   public async Task CreateFile()
   {
      LinesCreator linesCreator = new LinesCreator();
      byte[] line = new byte[_maxLineLength * _chunkSize];

      string fileName = @$"d://temp/ATT/{_fileName}.txt";
      await using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
      int start = 0;
      for (int i = 0; i < _lines; i++)
      {
         int length = linesCreator.NextLine(line, start);
         start += length;
         if (_maxLineLength * _chunkSize - start < _maxLineLength)
         {
            await fileStream.WriteAsync(line, 0, start);
            start = 0;
         }
      }

      if (start > 0)
         await fileStream.WriteAsync(line, 0, start);
   }
}