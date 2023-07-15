using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Sorters;

namespace SortingEngine;

public class BunchOfLinesSorter : IAsyncObserver<SortingPhasePackage>
{
    private readonly ILogger _logger;

    private readonly SimpleAsyncSubject<AfterSortingPhasePackage> _sortingCompletedSubject =
        new SequentialSimpleAsyncSubject<AfterSortingPhasePackage>();

    public BunchOfLinesSorter(ILogger logger)
    {
        _logger = Guard.NotNull(logger);
    }

    public IAsyncObservable<AfterSortingPhasePackage> SortingCompleted => _sortingCompletedSubject;
    // public event EventHandler<SortingCompletedEventArgs>? SortingCompleted;

    private LineMemory[] SortRecords(ReadOnlyMemory<byte> inputBuffer, int linesNumber,
        ExpandingStorage<LineMemory> recordsStorage)
    {
        //In the case of such a highly specific line comparison algorithm,
        //it makes no sense to add an interface and use DI
        //There is no chance that any other sorter will be within the scope of this task.
        //A mock for testing, in this case, is also not needed.
        LinesSorter sorter = new LinesSorter(inputBuffer);
        return sorter.Sort(recordsStorage, linesNumber);
    }

    public async ValueTask OnNextAsync(SortingPhasePackage inputPackage)
    {
        await Log(
                $"Processing package: {inputPackage.PackageNumber}, lines: {inputPackage.LinesNumber}, " +
                $"bytes: {inputPackage.RowData.Length},linesBuffer: {inputPackage.ParsedRecords.CurrentCapacity} ")
            .ConfigureAwait(false);
        await Task.Factory.StartNew<Task<bool>>(async (state) =>
                {
                    if (state == null) throw new ArgumentNullException(nameof(state));

                    SortingPhasePackage package = (SortingPhasePackage)state;
                    ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
                    LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

                    await Log($"Sorted {sorted.Length} lines for the package: {package.PackageNumber}")
                        .ConfigureAwait(false);
                    await _sortingCompletedSubject.OnNextAsync(new AfterSortingPhasePackage(sorted, package.RowData,
                            package.ParsedRecords, package.LinesNumber, package.PackageNumber, package.IsLastPackage))
                        .ConfigureAwait(false);
                    return true;
                }, inputPackage, CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default)
            .ConfigureAwait(false);
    }

    public async ValueTask OnErrorAsync(Exception error)
    {
        await _sortingCompletedSubject.OnCompletedAsync().ConfigureAwait(false);
    }

    public async ValueTask OnCompletedAsync()
    {
        await _sortingCompletedSubject.OnCompletedAsync().ConfigureAwait(false);
    }

    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"Class: {GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message).ConfigureAwait(false);
    }
}