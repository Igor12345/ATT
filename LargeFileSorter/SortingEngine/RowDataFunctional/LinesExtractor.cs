using SortingEngine.Algorithms;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowDataFunctional
{
   public struct PositionedSpan<T>
   {
      //see Microsoft.Toolkit.HighPerformance.Enumerables.SpanEnumerable<T>
      public PositionedSpan(ReadOnlyMemory<T> span, int from, int to)
      {
         Span = span;
         From = from;
         To = to;
      }

      public ReadOnlyMemory<T> Span { get; }
      public int From { get; }
      public int To { get; }
   } 
   
   public static class SpanExtensions{
      public static IEnumerable<PositionedSpan<T>> SplitOn<T>(this ReadOnlyMemory<T> span,
         Func<ReadOnlyMemory<T>, int> splitCondition)
      {
         int startLine = 0;
         do
         {
            int endCurrentLineNoEol = splitCondition(span[startLine..]);
            
            if (endCurrentLineNoEol <= 0)
               break;
            yield return new PositionedSpan<T>(span, startLine, endCurrentLineNoEol);
            startLine += endCurrentLineNoEol;

         } while (true);
      }
   }
   
   public class LinesExtractor
   {
      private readonly IParticularSubstringMatcher _eolFinder;
      private readonly RowData.LineParser _lineParser;

      private readonly int _eolLength;

      /// <summary>
      /// This class works only with bytes and knows nothing about encoding.
      /// It needs to know the byte sequences for the end of a line and for the delimiter between a number and a text to extract and then parse lines.
      /// </summary>
      /// <param name="eolFinder">The finder of the end of a line sequence</param>
      /// <param name="eolLength">The length of the eol bytes sequence. Must match encoding selected for eolFinred</param>
      /// <param name="lineParser">The line parser</param>
      public LinesExtractor(IParticularSubstringMatcher eolFinder, int eolLength, RowData.LineParser lineParser)
      {
         _eolFinder = NotNull(eolFinder);
         _eolLength = NotNull(eolLength);
         _lineParser = NotNull(lineParser);
      }

      public ExtractionResult ExtractRecords(ReadOnlySpan<byte> input, ExpandingStorage<Line> records, int offset = 0)
      {
         int lineIndex = 0;
         int startLine = 0;
         
         do
         {
            int endCurrentLineNoEol = _eolFinder.Find(input[startLine..]);
            if (endCurrentLineNoEol <= 0)
               break;
            int endLine = startLine + endCurrentLineNoEol + _eolLength;
            var range = startLine..endLine;
            Either<Error, Line> result = _lineParser.Parse(input[startLine..endLine]);
            
            
            result.Match(
               l => { return ExtractionResult.Ok()},
               e => { return ExtractionResult.Error(e.Message); }
            );
            if (result.IsLeft)
               return ExtractionResult.Error(result.L);
            
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
