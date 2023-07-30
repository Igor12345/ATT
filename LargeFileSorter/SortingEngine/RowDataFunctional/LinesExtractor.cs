using SortingEngine.Algorithms.OnMemory;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowDataFunctional
{
   public class LinesExtractor
   {
      private readonly IParticularSubstringMatcher _eolFinderFunc;
      private readonly LineParser _lineParser;

      /// <summary>
      /// This class works only with bytes and knows nothing about encoding.
      /// It needs to know the byte sequences for the end of a line and for the delimiter between a number and a text to extract and then parse lines.
      /// </summary>
      /// <param name="eolFinder">The finder of the end of a line sequence</param>
      /// <param name="lineParser">The line parser</param>
      public LinesExtractor(IParticularSubstringMatcher eolFinder, LineParser lineParser)
      {
         _eolFinderFunc = NotNull(eolFinder);
         _lineParser = NotNull(lineParser);
      }

      public Either<Error, int> ExtractRecords(ReadOnlyMemory<byte> input, ExpandingStorage<Line> records,
         int offset = 0)
      {
         Either<Error, int> result = input
            .SplitOn(_eolFinderFunc.Find)
            .Map(ParseLine)
            .Fold((Either<Error, int>)0, (state, lineOrTail)
               => Folder(records, state, lineOrTail));
            
         return result;
      }

      private Either<Error, int> Folder(ExpandingStorage<Line> records,
         Either<Error, int> state,
         Either<Error, Either<int, Line>> next)
      {
         return state.Match(
            Left: e => e,
            Right: last => next.Map(i => ConvertNext(i, last, records))
         );
      }

      private int ConvertNext(Either<int,Line> lineOrTail, int st, ExpandingStorage<Line> records)
      {
         return lineOrTail.Match(
            Left: tail => tail,
            Right: line =>
            {
               records.Add(line);
               return st;
            });
      }
      
      private Either<Error, Either<int, Line>> ParseLine(Either<int, PositionedSpan<byte>> either)
      {
         return either.Match(
            Left: tail => (Either<int, Line>)tail,
            Right: ps =>
            {
               Either<Error, Line> result = _lineParser.Parse(ps.Buffer.Span[ps.Start..(ps.Start + ps.Length)]);
               return result.Match<Either<Error, Either<int, Line>>>(
                  Right: parsedLine =>
                  {
                     Line line = parsedLine with
                     {
                        //todo lost offset !!!
                        From = parsedLine.From + ps.Start + 0, To = parsedLine.To + ps.Start + 0
                     };
                     return (Either<int, Line>)line;
                  },
                  Left: e => e);
            }
         );
      }

   }
}
