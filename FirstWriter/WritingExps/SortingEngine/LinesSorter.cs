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
        Sorters.LinesSorter sorter = new Sorters.LinesSorter(inputBuffer);
        return sorter.Sort(recordsStorage, linesNumber);
    }

    public async ValueTask OnNextAsync(SortingPhasePackage inputPackage)
    {
        await Log(
            $"Processing package: {inputPackage.PackageNumber}, lines: {inputPackage.LinesNumber}, " +
            $"bytes: {inputPackage.RowData.Length},linesBuffer: {inputPackage.ParsedRecords.CurrentCapacity} ");
        await Task.Factory.StartNew<Task<bool>>(async (state) =>
            {
                if (state == null) throw new ArgumentNullException(nameof(state));

                SortingPhasePackage package = (SortingPhasePackage)state;
                ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
                LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

                await Log($"Sorted {sorted.Length} lines for the package: {package.PackageNumber}");
                await _sortingCompletedSubject.OnNextAsync(new AfterSortingPhasePackage(sorted, package.RowData,
                    package.ParsedRecords, package.LinesNumber, package.PackageNumber));
                return true;
            }, inputPackage, CancellationToken.None,
            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);
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