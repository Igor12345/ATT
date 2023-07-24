using System.Text;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using SortingEngine.Algorithms;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData
{
   public class LinesExtractor
   {
      private readonly IParticularSubstringMatcher _eolFinder;
      private readonly LineParser _lineParser;

      private readonly int _eolLength;

      /// <summary>
      /// This class works only with bytes and knows nothing about encoding.
      /// It needs to know the byte sequences for the end of a line and for the delimiter between a number and a text to extract and then parse lines.
      /// </summary>
      /// <param name="eolFinder">The finder of the end of a line sequence</param>
      /// <param name="eolLength">The length of the eol bytes sequence. Must match encoding selected for eolFinred</param>
      /// <param name="lineParser">The line parser</param>
      public LinesExtractor(IParticularSubstringMatcher eolFinder, int eolLength, LineParser lineParser)
      {
         _eolFinder = Guard.NotNull(eolFinder);
         _eolLength = Guard.NotNull(eolLength);
         _lineParser = Guard.NotNull(lineParser);
      }

      public ExtractionResult ExtractRecords(ReadOnlySpan<byte> input, ExpandingStorage<Line> records, int offset = 0)
      {
         int lineIndex = 0;
         int endCurrentLineNoEol;
         int endLine;
         int startLine = 0;
         do
         {
            endCurrentLineNoEol = _eolFinder.Find(input[startLine..]);
            if (endCurrentLineNoEol <= 0)
               break;

            endLine = startLine + endCurrentLineNoEol + _eolLength;
            var result = _lineParser.Parse(input[startLine..endLine]);
            if (!result.Success)
               return ExtractionResult.Error(result.Message);
            
            Line line = result.Value with
            {
               From = result.Value.From + startLine + offset, To = result.Value.To + startLine + offset
            };
            records.Add(line);
            lineIndex++;
            startLine = endLine;
         } while (true);

         return ExtractionResult.Ok(lineIndex, startLine);
      }
   }
}
