using System.Reactive.Subjects;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Sorters;

namespace SortingEngine;

public class LinesSorter:IAsyncObserver<PreSortBuffer>
{
    private readonly SimpleAsyncSubject<SortingCompletedEventArgs> _sortingCompletedSubject =
        new SequentialSimpleAsyncSubject<SortingCompletedEventArgs>();

    private ExpandingStorage<LineMemory> _recordsStorage;

    public IAsyncObservable<SortingCompletedEventArgs> SortingCompleted => _sortingCompletedSubject;
    // public event EventHandler<SortingCompletedEventArgs>? SortingCompleted;
    
    public LineMemory[] SortRecords(ReadOnlyMemory<byte> inputBuffer, int linesNumber,
        ExpandingStorage<LineMemory> recordsStorage)
    {
        InSiteRecordsSorter sorter = new InSiteRecordsSorter(inputBuffer);
        return sorter.Sort(recordsStorage, linesNumber);
    }

    public async ValueTask OnNextAsync(PreSortBuffer inputBuffer)
    {
        LineMemory[] sorted = SortRecords(inputBuffer.Write, inputBuffer.LinesNumber, inputBuffer.RecordsStorage);
        await _sortingCompletedSubject.OnNextAsync(new SortingCompletedEventArgs(sorted, inputBuffer.Write));
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