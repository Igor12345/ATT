using SortingEngine.Comparators;
using SortingEngine.Entities;

namespace SortingEngine.Sorting
{
   public class RecordsSorter
   {
      public LineRecord[] Sort(LineRecord[] input)
      {
         return input.Order(new RecordsComparer()).ToArray();
      }
   }
}
