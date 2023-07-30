using SortingEngine.Algorithms.OnMemory;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowDataFunctional
{
   public interface ISpanFunc<TSp, out TOut>
   {
      TOut Find(ReadOnlySpan<TSp> text);
   }

   public class LinesExtractor
   {
      private readonly IParticularSubstringMatcher _eolFinderFunc;
      private readonly LineParser _lineParser;

      /// <summary>
      /// This class works only with bytes and knows nothing about encoding.
      /// It needs to know the byte sequences for the end of a line and for the delimiter between a number and a text to extract and then parse lines.
      /// </summary>
      /// <param name="eolFinder">The finder of the end of a line sequence</param>
      /// <param name="eolLength">The length of the eol bytes sequence. Must match encoding selected for eolFinder</param>
      /// <param name="lineParser">The line parser</param>
      public LinesExtractor(IParticularSubstringMatcher eolFinder, LineParser lineParser)
      {
         _eolFinderFunc = NotNull(eolFinder);
         _lineParser = NotNull(lineParser);
      }

      public Either<Error, int> ExtractRecords(ReadOnlyMemory<byte> input, ExpandingStorage<Line> records,
         int offset = 0)
      {
         // Either<Error, int> result1= input.SplitOn(_eolFinderFunc).Map(ParseLine).FoldT(0, (tail, lineOrTail) => 
         //    CreateAndAdd(records, tail, lineOrTail));
         var list = new List<Either<Error, int>>();
         var r = list.ToSeq().FoldT(0, (s, x) => s + x); 
            
         // var t = list.Fold(0, (s, x) => s + x);
         // var total = fold(list, 0, (s,x) => s + x);
         // Either<Error, int> result1 = input.SplitOn(_eolFinderFunc.Find).ToSeq().Map(ParseLine).Map(p => { return p;}).Fold()
         
         Either<Error, int> result = input.SplitOn(_eolFinderFunc.Find).ToSeq().Map(ParseLine).Map(p => { return p;}).FoldT(
            (Either<Error, int>)0,
            (Either<Error, int> tail, Either<int, Line> lineOrTail) => SaveLine(records, tail, lineOrTail));

         return result;
      }

      private Either<Error, int> SaveLine(ExpandingStorage<Line> records, Either<Error, int> result, Either<int, Line> lineOrTail)
      {
         return result.Match(
            Left: e => (Either<Error, int>)e,
            Right: lt =>
            {
               return lineOrTail.Match(
                  Left: tail => tail,
                  Right: line =>
                  {
                     records.Add(line);
                     return lt;
                  }
               );
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
