using System.Collections.Immutable;
using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Sorters;

namespace SortingEngine;

public class LinesSorter : IAsyncObserver<SortingPhasePackage>
{
    private readonly Logger _logger;

    private readonly SimpleAsyncSubject<AfterSortingPhasePackage> _sortingCompletedSubject =
        new SequentialSimpleAsyncSubject<AfterSortingPhasePackage>();

    public LinesSorter(Logger logger)
    {
        _logger = Guard.NotNull(logger);
    }

    public IAsyncObservable<AfterSortingPhasePackage> SortingCompleted => _sortingCompletedSubject;
    // public event EventHandler<SortingCompletedEventArgs>? SortingCompleted;

    public LineMemory[] SortRecords(ReadOnlyMemory<byte> inputBuffer, int linesNumber,
        ExpandingStorage<LineMemory> recordsStorage)
    {
        InSiteRecordsSorter sorter = new InSiteRecordsSorter(inputBuffer);
        return sorter.Sort(recordsStorage, linesNumber);
    }

    public async ValueTask OnNextAsync(SortingPhasePackage package)
    {
        await Log(
            $"Processing package: {package.PackageNumber}, lines: {package.LinesNumber}, bytes: {package.RowData.Length}, linesBuffer: {package.ParsedRecords.CurrentCapacity}");
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
        LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

        await Log($"Sorted {sorted.Length} lines for the package: {package.PackageNumber}");
        await _sortingCompletedSubject.OnNextAsync(new AfterSortingPhasePackage(sorted, package.RowData,
            package.ParsedRecords, package.LinesNumber, package.PackageNumber));
    }

    public async ValueTask OnErrorAsync(Exception error)
    {
        await _sortingCompletedSubject.OnCompletedAsync();
    }

    public async ValueTask OnCompletedAsync()
    {
        await _sortingCompletedSubject.OnCompletedAsync();
    }
    
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
}