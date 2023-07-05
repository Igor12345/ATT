using ContentGenerator;

namespace Benchmarks;

internal class SecondFakeExecutor
{
   private readonly string _fileName;
   private readonly long _lines;

   public SecondFakeExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
   }

   public int CreateFile()
   {
      LinesCreator linesCreator = new LinesCreator();
      byte[] line = new byte[19 + 4 + 1024];

      int result = 0;
      for (int i = 0; i < _lines; i++)
      {
         int length = linesCreator.NextLine(line);
         result+=length;
      }
      return result;
   }
}