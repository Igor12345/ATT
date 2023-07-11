using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.Sorters;

public class InSiteRecordsSorter
{
   private readonly ReadOnlyMemory<byte> _source;

   public InSiteRecordsSorter(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   public LineMemory[] Sort(LineMemory[] input)
   {
      Array.Sort(input, new InSiteRecordsComparer(_source));
      //todo !!! create new big array
      return input.Order(new InSiteRecordsComparer(_source)).ToArray();
   }

   public LineMemory[] Sort(ExpandingStorage<LineMemory> recordsPool, int resultSize)
   {
      LineMemory[] result = new LineMemory[resultSize];
      recordsPool.CopyTo(result, resultSize);
      Array.Sort(result, new InSiteRecordsComparer(_source));
      return result;
   }
}