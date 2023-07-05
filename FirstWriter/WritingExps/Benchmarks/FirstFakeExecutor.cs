using BenchmarkDotNet.Attributes;
using ContentGenerator;

namespace Benchmarks;

internal class FirstFakeExecutor
{
   private readonly string _fileName;
   private readonly long _lines;

   public FirstFakeExecutor(string fileName, long lines)
   {
      _fileName = fileName;
      _lines = lines;
   }

   public int CreateFile()
   {
      ITextCreator numbersCreator = TextCreator.NumbersCreator;
      TextCreator textCreator = new TextCreator();
      byte[] newLine = textCreator.Encoding.GetBytes(Environment.NewLine);
      byte[] splitter = textCreator.Encoding.GetBytes(". ");

      string fileName = @$"d://temp/ATT/{_fileName}.txt";
      
      using IEnumerator<byte[]> textBytesSource = textCreator.GenerateBytes().GetEnumerator();
      using IEnumerator<byte[]> numberBytesSource = numbersCreator.GenerateBytes().GetEnumerator();

      textBytesSource.MoveNext();
      numberBytesSource.MoveNext();

      int result = 0;
      for (int i = 0; i < _lines; i++)
      {
         List<byte> line = new List<byte>();
         line.AddRange(numberBytesSource.Current);
         line.AddRange(splitter);
         line.AddRange(textBytesSource.Current);
         line.AddRange(newLine);
         result++;
         textBytesSource.MoveNext();
         numberBytesSource.MoveNext();
      }

      return result;
   }
}