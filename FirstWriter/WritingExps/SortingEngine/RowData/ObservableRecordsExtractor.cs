using System.Buffers;
using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public sealed class ObservableRecordsExtractor 
{
    private readonly SimpleAsyncSubject<PreReadPackage> _readyForNextChunkSubject =
        new SequentialSimpleAsyncSubject<PreReadPackage>();
      
    private readonly ILogger _logger;
    private readonly RecordsExtractor _recordsExtractor;

    public ObservableRecordsExtractor(byte[] eol, byte[] lineDelimiter, ILogger logger, CancellationToken token)
    {
        _recordsExtractor = new RecordsExtractor(eol, lineDelimiter);
        _logger = Guard.NotNull(logger);
    }

    public async Task<(SortingPhasePackage,PreReadPackage)> ExtractNextAsync(ReadingPhasePackage package)
    {
        int i = package.PackageNumber;
        int id = package.RowData.GetHashCode();

        await Log(
            $"Processing package: {package.PackageNumber}, is last: {package.IsLastPackage}, " +
            $"bytes: {package.RowData.Length}, pre populated: {package.PrePopulatedBytesLength}, buffer id: {id}, thread: {Thread.CurrentThread.ManagedThreadId}");

        ExtractionResult result = _recordsExtractor.ExtractRecords(package.RowData.AsSpan()[..package.WrittenBytesLength],
            package.ParsedRecords);
        
        
        if (!result.Success)
        {
            await Log($"!!!! Extraction error {result.Success}: {result.Message}, sending readyForNextChunkSubject.OnErrorAsync ");
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
        }

        int remainingBytesLength = package.WrittenBytesLength - result.StartRemainingBytes;
        await Log($"Package: {package.PackageNumber} after extraction. Found {result.LinesNumber} lines, left {remainingBytesLength} bytes for the next chunk");

        //will be returned in SortingPhasePoolManager
        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.RowData.AsSpan()[result.StartRemainingBytes..package.WrittenBytesLength].CopyTo(remainedBytes);

        // if (package.PackageNumber == 3)
        // {
        //     ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.ReadBytesLength];
        //     LineMemory[] result2 = ArrayPool<LineMemory>.Shared.Rent(result.LinesNumber);
        //     package.ParsedRecords.CopyTo(result2, result.LinesNumber);
        //     
        //     var wrongLine = result2.FirstOrDefault(l => l.Number == 6748015574496075763||l.Number == 2415040422824707043||l.Number == 8633638752424593355);
        //     var cnt = result2.Count(l => l.Number == 6748015574496075763||l.Number == 2415040422824707043||l.Number == 8633638752424593355);
        //     if (cnt == 1)
        //     {
        //         var c = cnt;
        //     }
        //
        //     if (wrongLine.Number != 0 || cnt > 0)
        //     {
        //         string wrongStr = LinesUtils2.LineToString(wrongLine, package.RowData);
        //         Console.WriteLine();
        //         Console.WriteLine(
        //             $"!!!Wrong line {wrongStr}, in package {package.PackageNumber}, from: {wrongLine.From}.. to {wrongLine.To}");
        //         string bytes = string.Join(", ",
        //             package.RowData.Take(wrongLine.From..wrongLine.To).Select(b => (char)b));
        //         Console.WriteLine($"Bytes: {bytes}");
        //         Console.WriteLine();
        //     }
        // }
        
        //todo
        SortingPhasePackage nextPackage = new SortingPhasePackage(package.RowData, package.WrittenBytesLength,
            package.ParsedRecords, result.LinesNumber, package.PackageNumber, package.IsLastPackage);

        id = nextPackage.RowData.GetHashCode();
        await Log(
            $"Sending the SortingPhasePackage {nextPackage.PackageNumber}, extracted {nextPackage.LinesNumber}, " +
            $"bytes: {nextPackage.RowData.Length}, bufferId: {id}, linesBuffer: {nextPackage.ParsedRecords.CurrentCapacity}, " +
            $"used bytes: {nextPackage.OccupiedLength}, thread: {Thread.CurrentThread.ManagedThreadId}");
        
        // if (package.IsLastPackage)
        // {
        //     Console.WriteLine($"<**> From Extractor last package {package.PackageNumber}, !!! NOT closing ReadyForNextChunk");
        //     // await _readyForNextChunkSubject.OnCompletedAsync();
        // }
        // else
        // {
        //     await _readyForNextChunkSubject.OnNextAsync(new PreReadPackage(remainedBytes,
        //         remainingBytesLength));
        // }

        PreReadPackage preReadPackage = package.IsLastPackage
            ? PreReadPackage.LastPackage(package.PackageNumber)
            : new PreReadPackage(remainedBytes,
                remainingBytesLength, package.PackageNumber, false);
        await Log(
            $"Also Sending the PreReadPackage {preReadPackage.PackageNumber}, " +
            $"bytes: {preReadPackage.RemainedBytesLength}, thread: {Thread.CurrentThread.ManagedThreadId}");
        //todo!!!
        return (nextPackage, preReadPackage);
    }
      
    // public async ValueTask OnNextAsync(ReadingPhasePackage package)
    // {
    //     await Log(
    //         $"Processing package: {package.PackageNumber}, is last: {package.IsLastPackage}, " +
    //         $"bytes: {package.RowData.Length}, pre populated: {package.PrePopulatedBytesLength}");
    //
    //     ExtractionResult result = _recordsExtractor.ExtractRecords(package.RowData.AsSpan()[..package.ReadBytesLength],
    //         package.ParsedRecords);
    //     
    //     if (!result.Success)
    //     {
    //         await Log($"Extracted {result.Success}: {result.Message} ");
    //         await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
    //     }
    //
    //     int remainingBytesLength = package.ReadBytesLength - result.StartRemainingBytes;
    //
    //     //will be returned in SortingPhasePoolManager
    //     byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
    //     package.RowData.AsSpan()[result.StartRemainingBytes..package.ReadBytesLength].CopyTo(remainedBytes);
    //
    //     //todo
    //     SortingPhasePackage nextPackage = new SortingPhasePackage(package.RowData, package.ReadBytesLength,
    //         package.ParsedRecords, result.LinesNumber, package.PackageNumber, package.IsLastPackage);
    //
    //     await Log(
    //         $"Sending the package {nextPackage.PackageNumber}, extracted {nextPackage.LinesNumber}, " +
    //         $"bytes: {nextPackage.RowData.Length}, linesBuffer: {nextPackage.ParsedRecords.CurrentCapacity}, " +
    //         $"used bytes: {nextPackage.OccupiedLength}");
    //     //todo!!!
    //     // await _readyForSortingSubject.OnNextAsync(nextPackage);
    //     if (package.IsLastPackage)
    //     {
    //         await _readyForNextChunkSubject.OnCompletedAsync();
    //     }
    //     else
    //     {
    //         await _readyForNextChunkSubject.OnNextAsync(new PreReadPackage(remainedBytes,
    //             remainingBytesLength));
    //     }
    // }

    // public ValueTask OnErrorAsync(Exception error)
    // {
    //     return _readyForNextChunkSubject.OnCompletedAsync();
    // }
    //
    // public ValueTask OnCompletedAsync()
    // {
    //     //we will complete this sequence as well, in such case there is nothing to do. Something went wrong
    //     return _readyForNextChunkSubject.OnCompletedAsync();
    // }
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
}