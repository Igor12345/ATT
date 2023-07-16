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
        Console.WriteLine($"---> Inside Sorter for {package.PackageNumber}, is last: {package.IsLastPackage}");
        await Log(
            $"Processing package: {package.PackageNumber}, lines: {package.LinesNumber}, " +
            $"bytes: {package.RowData.Length},linesBuffer: {package.ParsedRecords.CurrentCapacity} ");
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
        LineMemory[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

        if (package.LinesNumber > 0)
        {
            string first = LinesUtils2.LineToString(sorted[0], package.RowData);
            //todo remove
            if (first.StartsWith("4542039177020542"))
            {
                var d = first;
            }

            var wrongLine = sorted.FirstOrDefault(l => l.Number == 6748015574496075763||l.Number == 2415040422824707043||l.Number == 8633638752424593355);
            var cnt = sorted.Count(l => l.Number == 6748015574496075763||l.Number == 2415040422824707043||l.Number == 8633638752424593355);
            if (cnt == 1)
            {
                var c = cnt;
            }

            if (wrongLine.Number != 0 || cnt > 0)
            {
                string wrongStr = LinesUtils2.LineToString(wrongLine, package.RowData);
                Console.WriteLine();
                Console.WriteLine(
                    $"!!!Wrong line {wrongStr}, in package {package.PackageNumber}, from: {wrongLine.From}.. to {wrongLine.To}");
                string bytes = string.Join(", ",
                    package.RowData.Take(wrongLine.From..wrongLine.To).Select(b => (char)b));
                Console.WriteLine($"Bytes: {bytes}");
                Console.WriteLine();
            }
            else
            {
                if (package.PackageNumber == 3)
                {
                    var b = package.LinesNumber;
                    string l0 = LinesUtils2.LineToString(sorted[0], package.RowData);
                    string l1 = LinesUtils2.LineToString(sorted[1], package.RowData);
                    string l2 = LinesUtils2.LineToString(sorted[2], package.RowData);
                    string l3 = LinesUtils2.LineToString(sorted[3], package.RowData);
                }
            }
        }

        await Log($"Sorted {sorted.Length} lines for the package: {package.PackageNumber}");
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
        string prefix = $"Class: {GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
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