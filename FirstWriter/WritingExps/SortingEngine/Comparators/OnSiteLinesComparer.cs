using SortingEngine.Entities;

namespace SortingEngine.Comparators;

public class OnSiteLinesComparer : IComparer<LineMemory>
{
   private readonly ReadOnlyMemory<byte> _source;

   public OnSiteLinesComparer(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   public int Compare(LineMemory left, LineMemory right)
   {
      int orderByText =
         StringAsBytesComparer.Compare(_source[left.From..left.To].Span, _source[right.From..right.To].Span);
      if (orderByText != 0)
         return orderByText;

      return left.Number.CompareTo(right.Number);
   }
}