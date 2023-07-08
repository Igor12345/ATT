using System.Buffers;
using SortingEngine.Entities;

namespace SortingEngine.RowData
{
   public class RecordsExtractor
   {
      private readonly byte[] _eol;
      private readonly byte[] _lineDelimiter;

      public RecordsExtractor(byte[] eol, byte[] lineDelimiter)
      {
         _eol = eol;
         _lineDelimiter = lineDelimiter;
      }

      public void SplitOnLines(Span<byte> buffer, Span<int> linesPositions)
      {
         //compare with 
         // foreach (byte b in buffer)
         // {
         //    
         // }
         int lineIndex = 0;
         for (int i = 0; i < buffer.Length - 1; i++)
         {
            if (buffer[i] == _eol[0] && buffer[i + 1] == _eol[1])
            {
               linesPositions[lineIndex++] = i;
               i++;
            }
         }
      }

      public Result SplitOnRecords(Span<byte> input, LineRecord[] records)
      {
         int lineIndex = 0;
         int endLine = 0;
         for (int i = 0; i < input.Length - 1; i++)
         {
            if (input[i] == _eol[0] && input[i + 1] == _eol[1])
            {
               var startLine = endLine;
               endLine = i;
               LineRecord line = ExtractRecord(input[startLine..endLine]);
               records[lineIndex++] = line;
               i++;

               //todo
               if (lineIndex >= records.Length)
                  return new Result(true, "");
            }
         }

         return new Result(true, "");
      }
      public Result SplitOnMemoryRecords(byte[] input, LineMemory[] records)
      {
         int lineIndex = 0;
         int endLine = 0;
         for (int i = 0; i < input.Length - 1; i++)
         {
            if (input[i] == _eol[0] && input[i + 1] == _eol[1])
            {
               var startLine = endLine;
               endLine = i;
               LineMemory line = ExtractMemoryRecord(input[startLine..endLine], startLine);
               records[lineIndex++] = line;
               i++;

               //todo
               if (lineIndex >= records.Length)
                  return new Result(true, "");
            }
         }

         return new Result(true, "");
      }

      private LineRecord ExtractRecord(Span<byte> lineSpan)
      {
         for (int i = 0; i < lineSpan.Length-1; i++)
         {
            if (lineSpan[i] == _lineDelimiter[0] && lineSpan[i + 1] == _lineDelimiter[1])
            {
               Span<char> chars = new char[i];
               for (int j = 0; j < i; j++)
               {
                  //todo encoding
                  chars[j] = (char)lineSpan[j];
               }
               var success = long.TryParse(chars, out long number);
               //todo !success
               return new LineRecord(number, lineSpan[(i + 2)..].ToArray());
            }
         }

         //todo
         throw new InvalidOperationException($"wrong line {lineSpan.ToString()}");

      }

      private LineMemory ExtractMemoryRecord(Span<byte> lineSpan, int startIndex)
      {
         for (int i = 0; i < lineSpan.Length - 1; i++)
         {
            if (lineSpan[i] == _lineDelimiter[0] && lineSpan[i + 1] == _lineDelimiter[1])
            {
               char[] chars = ArrayPool<char>.Shared.Rent(i);
               for (int j = 0; j < i; j++)
               {
                  //todo encoding
                  chars[j] = (char)lineSpan[j];
               }

               bool success = long.TryParse(chars, out long number);
               ArrayPool<char>.Shared.Return(chars);
               //todo !success
               return new LineMemory(number, startIndex + i + 2, startIndex + lineSpan.Length);
            }
         }

         //todo
         throw new InvalidOperationException($"wrong line {lineSpan.ToString()}");

      }

      private void AddRecord(Span<byte> line, int startLine, int endLine, LineRecord[] records)
      {
         
      }
   }
}
