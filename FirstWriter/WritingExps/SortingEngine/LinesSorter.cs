using System.Reactive.Subjects;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Sorters;

namespace SortingEngine;

public class LinesSorter:IAsyncObserver<SortingPhasePackage>
{
    private readonly SimpleAsyncSubject<AfterSortingPhasePackage> _sortingCompletedSubject =
        new SequentialSimpleAsyncSubject<AfterSortingPhasePackage>();

    private ExpandingStorage<LineMemory> _recordsStorage;

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
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
        LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);
        await _sortingCompletedSubject.OnNextAsync(new AfterSortingPhasePackage(sorted, package.RowData,
            package.ParsedRecords, package.LinesNumber));
    }

    public async ValueTask OnErrorAsync(Exception error)
    {
        await _sortingCompletedSubject.OnCompletedAsync();
    }

    public async ValueTask OnCompletedAsync()
    {
        await _sortingCompletedSubject.OnCompletedAsync();
    }
}