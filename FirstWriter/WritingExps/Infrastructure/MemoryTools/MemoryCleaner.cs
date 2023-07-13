using System.Runtime;

namespace Infrastructure.MemoryTools;

public class MemoryCleaner
{
    public static void CleanMemory()
    {
        for (int i = 0; i < 10; i++)
        {
            // Garbage collect as much as GC-able objects as possible.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();
        }

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
        // GC.Collect(2, GCCollectionMode.Aggressive, true, true);
    }
}