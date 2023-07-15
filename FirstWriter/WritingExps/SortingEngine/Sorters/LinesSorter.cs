using System.Buffers;
using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.Sorters;

public class LinesSorter
{
   private readonly ReadOnlyMemory<byte> _source;

   public LinesSorter(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   public LineMemory[] Sort(LineMemory[] input)
   {
      Array.Sort(input, new OnSiteLinesComparer(_source));
      //todo !!! create new big array
      return input.Order(new OnSiteLinesComparer(_source)).ToArray();
   }

   public LineMemory[] Sort(ExpandingStorage<LineMemory> recordsPool, int linesNumber)
   {
      LineMemory[] result = ArrayPool<LineMemory>.Shared.Rent(linesNumber);
      recordsPool.CopyTo(result, linesNumber);
      Array.Sort(result, 0, linesNumber, new OnSiteLinesComparer(_source));
      return result;
   }
}