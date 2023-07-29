using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SortingEngine.Sorting;

public class SetOfLinesSorter
{
    private readonly Func<ReadOnlyMemory<byte>, ILinesSorter> _sorterFactory;
    private readonly ILogger _logger;

    public SetOfLinesSorter(ILogger logger, Func<ReadOnlyMemory<byte>, ILinesSorter> sorterFactory)
    {
        _logger = NotNull(logger);
        _sorterFactory = NotNull(sorterFactory);
    }

    public async Task<AfterSortingPhasePackage> ProcessPackageAsync(SortingPhasePackage package)
    {
        Line[] sorted = SortLines(package.LineData, package.LinesNumber, package.ParsedRecords);

        await Log(
            $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) Sorted lines: {package.LinesNumber}) " +
            $"for the package: {package.Id}, sending AfterSortingPhasePackage");
        return new AfterSortingPhasePackage(package, sorted);
    }

    private Line[] SortLines(ReadOnlyMemory<byte> inputBuffer, int linesNumber,
        ExpandingStorage<Line> recordsStorage)
    {
        //In the case of such a highly specific line comparison algorithm,
        //it makes no sense to add an interface and use DI
        //There is no chance that any other sorter will be within the scope of this task.
        //A mock for testing, in this case, is also not needed.
        ILinesSorter sorter = _sorterFactory(inputBuffer);
        return sorter.Sort(recordsStorage, linesNumber);
    }

    // in a real project, working with logs will look completely different
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{GetType()}, at: {DateTime.Now:hh:mm:ss-fff}, ({Thread.CurrentThread.ManagedThreadId})";
        await _logger.LogAsync(prefix + message);
    }
}