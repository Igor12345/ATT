using SortingEngine.Entities;

namespace SortingEngine.Comparators;

public class OnSiteLinesComparer : IComparer<Line>
{
   private readonly ReadOnlyMemory<byte> _source;

   public OnSiteLinesComparer(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   /// <summary>
   /// Provides string comparison based on the required algorithm.
   /// The line left is compared to the line right to determine
   /// whether it is less, equal, or greater, and then returns.
   /// either a negative integer, 0, or a positive integer; respectively.
   /// The lines are compared firstly by their text and then, it texts are equal, by their numbers 
   /// </summary>
   public int Compare(Line left, Line right)
   {
      int orderByText =
         StringAsBytesComparer.Compare(_source[left.From..left.To].Span, _source[right.From..right.To].Span);
      if (orderByText != 0)
         return orderByText;

      return left.Number.CompareTo(right.Number);
   }
}