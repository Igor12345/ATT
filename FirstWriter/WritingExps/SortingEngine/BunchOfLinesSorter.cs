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

    // public async ValueTask OnNextAsync(SortingPhasePackage inputPackage)
    // {
    //     await Log(
    //         $"Processing package: {inputPackage.PackageNumber}, lines: {inputPackage.LinesNumber}, " +
    //         $"bytes: {inputPackage.RowData.Length},linesBuffer: {inputPackage.ParsedRecords.CurrentCapacity} ");
    //     await Task.Factory.StartNew<Task<bool>>(async (state) =>
    //         {
    //             if (state == null) throw new ArgumentNullException(nameof(state));
    //
    //             SortingPhasePackage package = (SortingPhasePackage)state;
    //             ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
    //             LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);
    //
    //             await Log($"Sorted {sorted.Length} lines for the package: {package.PackageNumber}");
    //             await _sortingCompletedSubject.OnNextAsync(new AfterSortingPhasePackage(sorted, package.RowData,
    //                 package.ParsedRecords, package.LinesNumber, package.PackageNumber, package.IsLastPackage));
    //             return true;
    //         }, inputPackage, CancellationToken.None,
    //         TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);
    // }
    
    public async Task<AfterSortingPhasePackage> ProcessPackage(SortingPhasePackage package)
    {
        int id = package.RowData.GetHashCode();
        Console.WriteLine(
            $"---> Inside Sorter for {package.PackageNumber}, is last: {package.IsLastPackage}, buffer Id: {id}, thread: {Thread.CurrentThread.ManagedThreadId}");
        await Log(
            $"Processing package: {package.PackageNumber}, lines: {package.LinesNumber}, " +
            $"bytes: {package.OccupiedLength}, buffer Id: {id}, linesBuffer: {package.ParsedRecords.CurrentCapacity} ");
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
        LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

        //todo remove
        // if (package.PackageNumber > 1)
        // {
        //     string first = LinesUtils2.LineToString(sorted[0], package.RowData);
        //
        //     var sortedStrings = sorted.Select(l => LinesUtils2.LineToString(l, package.RowData)).ToArray();
        //     LineMemory[] result = ArrayPool<LineMemory>.Shared.Rent(package.LinesNumber);
        //     package.ParsedRecords.CopyTo(result, package.LinesNumber);
        //     
        //     var originalStrings = result.Select(l => LinesUtils2.LineToString(l, package.RowData)).ToArray();
        //
        //     var t = originalStrings;
        // }

        await Log(
            $"Sorted {sorted.Length} (in fact: {package.LinesNumber}) " +
            $"lines for the package: {package.PackageNumber}, sending AfterSortingPhasePackage, thread: {Thread.CurrentThread.ManagedThreadId}");
        return new AfterSortingPhasePackage(sorted, package.RowData,
            package.ParsedRecords, package.LinesNumber, package.PackageNumber, package.IsLastPackage);
    }

    // public async ValueTask OnErrorAsync(Exception error)
    // {
    //     await _sortingCompletedSubject.OnCompletedAsync();
    // }
    //
    // public async ValueTask OnCompletedAsync()
    // {
    //     await _sortingCompletedSubject.OnCompletedAsync();
    // }

    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
}

//todo !!! remove
public static class LinesUtils2
{
    public static string LineToString(LineMemory line, byte[] source)
    {
        Span<byte> buffer = stackalloc byte[Constants.MaxLineLengthUtf8];
        int length = LineToBytes(line, source, buffer);
 
        return ByteToStringConverter.Convert(buffer[..length]);
    }

    public static int LineToBytes(LineMemory line, ReadOnlySpan<byte> source, Span<byte> destination)
    {
        byte[]? rented = null;
        Span<byte> buffer = Constants.MaxLineLengthUtf8 <= Constants.MaxStackLimit
            ? stackalloc byte[Constants.MaxLineLengthUtf8]
            : rented = ArrayPool<byte>.Shared.Rent(Constants.MaxLineLengthUtf8);
        
        int length = LongToBytesConverter.WriteULongToBytes(line.Number, buffer);

        int fullLength = length + line.To - line.From;
        source[line.From..line.To].CopyTo(buffer[length..]);
        buffer.CopyTo(destination);
        if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);

        return fullLength;
    }
}