using SortingEngine.Entities;

namespace SortingEngine;

public class PoolsManager
{
   public LineMemory[] AcquireRecordsArray()
   {
      return new LineMemory[2_000_000];
   }
}