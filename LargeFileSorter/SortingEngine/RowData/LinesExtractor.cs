using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData
{
   //todo two responsibilities here
   public class LinesExtractor
   {
      private readonly byte[] _eol;
      private readonly LineParser _parser;

      /// <summary>
      /// This class works only with bytes and knows nothing about encoding.
      /// It needs to know the byte sequences for the end of a line and for the delimiter between a number and a text to extract and then parse lines.
      /// </summary>
      /// <param name="eol">The byte sequence of the end of a line in the used encoding</param>
      /// <param name="parser">The line parser</param>
      public LinesExtractor(byte[] eol, LineParser parser)
      {
         _eol = Guard.NotNull(eol);
         _parser = Guard.NotNull(parser);
      }

      //this is hardcoded for UTF-8
      public ExtractionResult ExtractRecords(ReadOnlySpan<byte> input, ExpandingStorage<Line> records,
         int offset = 0)
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
               
               //text will include eof. the question with the last line.
               endLine = i + 2;
               var result = _parser.Parse(input[startLine..endLine], startLine);

               if (!result.Success)
                  return ExtractionResult.Error(result.Message);

               Line line = result.Value with { From = result.Value.From + offset, To = result.Value.To + offset };
               records.Add(line);
               lineIndex++;
               i++;
            }
         }

         return ExtractionResult.Ok(lineIndex, endOfLastLine + 1);
      }
   }
}
