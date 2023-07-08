using SortingEngine.Comparators;
using SortingEngine.Entities;

namespace SortingEngine.Sorters
{
   public class RecordsSorter
   {
      public LineRecord[] Sort(LineRecord[] input)
      {
         return input.Order(new RecordsComparer()).ToArray();
      }
   }
}
