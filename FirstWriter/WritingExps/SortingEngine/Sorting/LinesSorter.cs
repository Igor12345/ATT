using System.Buffers;
using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.Sorting;

public interface ILinesSorter
{
   LineMemory[] Sort(ExpandingStorage<LineMemory> recordsPool, int linesNumber);
}

public sealed class LinesSorter : ILinesSorter
{
   private readonly ReadOnlyMemory<byte> _source;

   public LinesSorter(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   //todo use MemoryOwner
   public LineMemory[] Sort(ExpandingStorage<LineMemory> recordsPool, int linesNumber)
   {
      //the array will be returned to ArrayPool after saving it. It is the responsibility of the writer
      //Yes, this is a violation of SRP
      LineMemory[] result = ArrayPool<LineMemory>.Shared.Rent(linesNumber);
      recordsPool.CopyTo(result, linesNumber);
      Array.Sort(result, 0, linesNumber, new OnSiteLinesComparer(_source));
      return result;
   }
}