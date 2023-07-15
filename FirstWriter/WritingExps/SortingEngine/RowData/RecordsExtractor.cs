using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData
{
   //todo two responsibilities
   public class RecordsExtractor
   {
      private readonly byte[] _eol;
      private readonly byte[] _lineDelimiter;
      
      /// <summary>
      /// This class works only with bytes and knows nothing about encoding.
      /// It needs to know the byte sequences for the end of a line and for the delimiter between a number and a text to extract and then parse lines.
      /// </summary>
      /// <param name="eol">The byte sequence of the end of a line in the used encoding</param>
      /// <param name="lineDelimiter">The byte sequence of the delimiter in the used encoding</param>
      public RecordsExtractor(byte[] eol, byte[] lineDelimiter)
      {
         _eol = Guard.NotNull(eol);
         _lineDelimiter = Guard.NotNull(lineDelimiter);
      }

      public ExtractionResult ExtractRecords(ReadOnlySpan<byte> input, ExpandingStorage<LineMemory> records)
      {
         int lineIndex = 0;
         int endLine = 0;
         int endOfLastLine = -1;
         for (int i = 0; i < input.Length - 1; i++)
         {
            if (input[i] == _eol[0] && input[i + 1] == _eol[1])
            {
               endOfLastLine = i + 1;
               var startLine = endLine;
               //todo
               //text will include eof. the question with the last line.
               endLine = i + 2;
               var result = ParseLine(input[startLine..endLine], startLine);

               if (!result.Success)
                  return ExtractionResult.Error(result.Message);
               
               records.Add(result.Value);
               lineIndex++;
               i++;
            }
         }
         return ExtractionResult.Ok(lineIndex, endOfLastLine + 1);
      }

      private Result<LineMemory> ParseLine(ReadOnlySpan<byte> lineSpan, int startIndex)
      {
         Span<char> numberChars = stackalloc char[Constants.MaxNumberLength];
         for (int i = 0; i < lineSpan.Length - 1; i++)
         {
            if (lineSpan[i] == _lineDelimiter[0] && lineSpan[i + 1] == _lineDelimiter[1])
            {
               if (i >= numberChars.Length)
                  break;
               for (int j = 0; j < i; j++)
               {
                  //todo encoding
                  numberChars[j] = (char)lineSpan[j];
               }

               bool success = ulong.TryParse(numberChars, out var number);
               if (!success)
                  return Result<LineMemory>.Error($"wrong line: {ByteToStringConverter.Convert(lineSpan)}");

               //text will include ". "
               return Result<LineMemory>.Ok(new LineMemory(number, startIndex + i, startIndex + lineSpan.Length));
            }
         }

         return Result<LineMemory>.Error($"wrong line: {ByteToStringConverter.Convert(lineSpan)}");
      }

      //todo remove
      #region Chars records

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
               var success = ulong.TryParse(chars, out ulong number);
               //todo !success
               return new LineRecord(number, lineSpan[(i + 2)..].ToArray());
            }
         }

         //todo
         throw new InvalidOperationException($"wrong line {lineSpan.ToString()}");

      }

      #endregion

   }
}
