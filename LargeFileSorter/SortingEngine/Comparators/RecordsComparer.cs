using SortingEngine.Entities;

namespace SortingEngine.Comparators;

internal class RecordsComparer : IComparer<LineRecord>
{
   public int Compare(LineRecord left, LineRecord right)
   {
      int orderByText = StringAsBytesComparer.Compare(left.Text, right.Text);
      if (orderByText != 0)
         return orderByText;

      return left.Number.CompareTo(right.Number);
   }
}