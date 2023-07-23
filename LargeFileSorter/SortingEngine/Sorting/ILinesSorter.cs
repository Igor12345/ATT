using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.Sorting;

public interface ILinesSorter
{
    Line[] Sort(ExpandingStorage<Line> recordsPool, int linesNumber);
}