using Infrastructure.Parameters;
using SortingEngine.Entities;

namespace SortingEngine;

public class SortingCompletedEventArgs : EventArgs
{
   public SortingCompletedEventArgs(LineMemory[] sorted, ReadOnlyMemory<byte> source)
   {
      Sorted = Guard.NotNull(sorted);
      Source = source;
   }

   public LineMemory[] Sorted { get; init; }
   public ReadOnlyMemory<byte> Source { get; init; }
}