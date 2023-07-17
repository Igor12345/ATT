using System.Buffers;
using System.Diagnostics;
using System.Reactive.Subjects;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Sorting;

namespace SortingEngine;

public class BunchOfLinesSorter //: IAsyncObserver<SortingPhasePackage>
{
    private readonly ILogger _logger;

    // private readonly SimpleAsyncSubject<AfterSortingPhasePackage> _sortingCompletedSubject =
    //     new SequentialSimpleAsyncSubject<AfterSortingPhasePackage>();

    public BunchOfLinesSorter(ILogger logger)
    {
        _logger = Guard.NotNull(logger);
    }

    // public IAsyncObservable<AfterSortingPhasePackage> SortingCompleted => _sortingCompletedSubject;
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
    
    public async Task<AfterSortingPhasePackage> ProcessPackage(SortingPhasePackage package)
    {
        //todo
        // int id = package.RowData.GetHashCode();
        // Console.WriteLine(
        //     $"---> Inside Sorter for {package.PackageNumber}, is last: {package.IsLastPackage}, buffer Id: {id}, thread: {Thread.CurrentThread.ManagedThreadId}");
        // await Log(
        //     $"Processing package: {package.PackageNumber}, lines: {package.LinesNumber}, " +
        //     $"bytes: {package.OccupiedLength}, buffer Id: {id}, linesBuffer: {package.ParsedRecords.CurrentCapacity} ");
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
        LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

        await Log(
            $"Sorted {sorted.Length} (in fact: {package.LinesNumber}) " +
            $"lines for the package: {package.PackageNumber}, sending AfterSortingPhasePackage, thread: {Thread.CurrentThread.ManagedThreadId}");
        return new AfterSortingPhasePackage(sorted, package.RowData,
            package.ParsedRecords, package.LinesNumber, package.PackageNumber, package.IsLastPackage);
    }

    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
}