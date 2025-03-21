﻿using ContentGenerator;

namespace SimpleWriter
{
   internal class FirstExecutor 
   {
      private readonly string _fileName;
      private readonly long _lines;

      public FirstExecutor(string fileName, long lines)
      {
         _fileName = fileName;
         _lines = lines;
      }

      public async Task CreateFile()
      {
         ITextCreator numbersCreator = TextCreator.NumbersCreator;
         TextCreator textCreator = new TextCreator();
         byte[] newLine = textCreator.Encoding.GetBytes(Environment.NewLine);
         byte[] splitter = textCreator.Encoding.GetBytes(". ");

         string fileName = @$"d://temp/ATT/{_fileName}.txt";
         await using FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);

         using IEnumerator<byte[]> textBytesSource = textCreator.GenerateBytes().GetEnumerator();
         using IEnumerator<byte[]> numberBytesSource = numbersCreator.GenerateBytes().GetEnumerator();


         textBytesSource.MoveNext();
         numberBytesSource.MoveNext();

         for (int i = 0; i < _lines; i++)
         {
            List<byte> line = new List<byte>();
            line.AddRange(numberBytesSource.Current);
            line.AddRange(splitter);
            line.AddRange(textBytesSource.Current);
            line.AddRange(newLine);
            await fileStream.WriteAsync(line.ToArray());
            textBytesSource.MoveNext();
            numberBytesSource.MoveNext();
         }
      }
   }
}
