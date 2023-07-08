using System.Text;
using ContentGenerator;

namespace SimpleWriter;

internal class FourthExecutor
{
   private readonly string _fileName;
   private readonly long _lines;
   private readonly int _maxLineLength;
   private readonly int _chunkSize = 15;

   public FourthExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
      _maxLineLength = ConstValues.MaxNumberLength + 4 + ConstValues.MaxTextLength;
   }

   public void CreateFile()
   {
      using LinesCreator linesCreator = new LinesCreator();
      Span<byte> line = new byte[_maxLineLength * _chunkSize];

      string fileName = @$"d://temp/ATT/{_fileName}.txt";
      using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
      using BinaryWriter writer = new BinaryWriter(fileStream, Encoding.UTF8);
      int start = 0;

      // Console.WriteLine("----------------------");
      // Console.WriteLine("----------------------");
      for (int i = 0; i < _lines; i++)
      {
         int length = linesCreator.NextLine(line[start..]);
         // Console.WriteLine($"New line {i}; start: {start}, length: {length}");
         start += length;
         if (_maxLineLength * _chunkSize - start < _maxLineLength)
         {
            // Console.WriteLine($"--> Write to disk {i}; start (length): {start}");
            writer.Write(line.Slice(0, start));
            start = 0;
         }
      }
      // Console.WriteLine("----------------------");
      // Console.WriteLine($"<---> After loop; start (length): {start}");
      if (start > 0)
      {
         // Console.WriteLine($"--> Write to disk after loop; start: {start}");
         writer.Write(line[..start]);
      }
   }
}