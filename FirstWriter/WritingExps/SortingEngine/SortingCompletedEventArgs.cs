using Infrastructure.Parameters;
using SortingEngine.Entities;

namespace SortingEngine;

public class SortingCompletedEventArgs : EventArgs
{
   public SortingCompletedEventArgs(LineMemory[] sorted, int linesNumber, ReadOnlyMemory<byte> source)
   {
      Sorted = Guard.NotNull(sorted);
      LinesNumber = Guard.Positive(linesNumber);
      Source = source;
   }

   public LineMemory[] Sorted { get; init; }
   public int LinesNumber { get; init; }
   public ReadOnlyMemory<byte> Source { get; init; }
}

public class PointEventArgs : EventArgs
{
   public PointEventArgs(string name)
   {
      Name = name;
   }
   public string Name { get; set; }
}