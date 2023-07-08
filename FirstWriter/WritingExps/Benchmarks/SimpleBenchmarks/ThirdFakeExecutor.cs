using BenchmarkDotNet.Attributes;
using ContentGenerator;

namespace Benchmarks;

internal class ThirdFakeExecutor
{
   private readonly string _fileName;
   private readonly long _lines;
   private readonly int _maxLineLength;
   private readonly int _chunkSize = 10_000;

   public ThirdFakeExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
      _maxLineLength = ConstValues.MaxNumberLength + 4 + ConstValues.MaxTextLength;
   }

   [Benchmark(Baseline = true)]
   public int CreateFile()
   {
      LinesCreator linesCreator = new LinesCreator();
      byte[] line = new byte[_maxLineLength * _chunkSize];

      string fileName = @$"d://temp/ATT/{_fileName}.txt";
      int start = 0;
      int length;
      int result = 0;
      for (int i = 0; i < _lines; i++)
      {
         length = linesCreator.NextLine(line, start);
         start += length;
         if (_maxLineLength * _chunkSize - start < _maxLineLength)
         {
            result+=start;
            start = 0;
         }
      }
      return result;
   }
}