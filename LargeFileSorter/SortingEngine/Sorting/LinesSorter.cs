using System.Buffers;
using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.Sorting;

public interface ILinesSorter
{
   Line[] Sort(ExpandingStorage<Line> recordsPool, int linesNumber);
}

public sealed class LinesSorter : ILinesSorter
{
   private readonly ReadOnlyMemory<byte> _source;

   public LinesSorter(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   //todo use MemoryOwner
   public Line[] Sort(ExpandingStorage<Line> recordsPool, int linesNumber)
   {
      //the array will be returned to ArrayPool after saving it. It is the responsibility of the writer
      //Yes, this is a violation of SRP
      Line[] result = ArrayPool<Line>.Shared.Rent(linesNumber);
      recordsPool.CopyTo(result, linesNumber);
      Array.Sort(result, 0, linesNumber, new OnSiteLinesComparer(_source));
      return result;
   }
}