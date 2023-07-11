using SortingEngine.Comparators;
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
      //todo !!! create new big array
      return input.Order(new InSiteRecordsComparer(_source)).ToArray();
   }
}