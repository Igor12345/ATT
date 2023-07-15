using System.Buffers;
using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;

namespace SortingEngine.RowData;

public sealed class ObservableRecordsExtractor : IAsyncObserver<ReadingPhasePackage>
{
    private readonly CancellationToken _token;
    private readonly SimpleAsyncSubject<PreReadPackage> _readyForNextChunkSubject =
        new SequentialSimpleAsyncSubject<PreReadPackage>();
    private readonly SimpleAsyncSubject<SortingPhasePackage> _readyForSortingSubject = 
        new SequentialSimpleAsyncSubject<SortingPhasePackage>();
      
    private readonly ILogger _logger;
    private readonly RecordsExtractor _recordsExtractor;

    public ObservableRecordsExtractor(byte[] eol, byte[] lineDelimiter, ILogger logger, CancellationToken token)
    {
        _recordsExtractor = new RecordsExtractor(eol, lineDelimiter);
        _logger = Guard.NotNull(logger);
        _token = Guard.NotNull(token);
        
    }

    public IAsyncObservable<PreReadPackage> ReadyForNextChunk => _readyForNextChunkSubject;
    public IAsyncObservable<SortingPhasePackage> ReadyForSorting => _readyForSortingSubject;

    private async Task ExtractNext(ReadingPhasePackage package)
    {
        await Log(
            $"Processing package: {package.PackageNumber}, is last: {package.IsLastPackage}, " +
            $"bytes: {package.RowData.Length}, pre populated: {package.PrePopulatedBytesLength}");

        ExtractionResult result = _recordsExtractor.ExtractRecords(package.RowData.AsSpan()[..package.ReadBytesLength],
            package.ParsedRecords);
        
        if (!result.Success)
        {
            await Log($"Extracted {result.Success}: {result.Message} ");
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
        }

        int remainingBytesLength = package.ReadBytesLength - result.StartRemainingBytes;

        //will be returned in SortingPhasePoolManager
        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.RowData.AsSpan()[result.StartRemainingBytes..package.ReadBytesLength].CopyTo(remainedBytes);

        //todo
        SortingPhasePackage nextPackage = new SortingPhasePackage(package.RowData, package.ReadBytesLength,
            package.ParsedRecords, result.LinesNumber, package.PackageNumber, package.IsLastPackage);

        await Log(
            $"Sending the package {nextPackage.PackageNumber}, extracted {nextPackage.LinesNumber}, " +
            $"bytes: {nextPackage.RowData.Length}, linesBuffer: {nextPackage.ParsedRecords.CurrentCapacity}, " +
            $"used bytes: {nextPackage.OccupiedLength}");
        //todo!!!
        await _readyForSortingSubject.OnNextAsync(nextPackage);
        if (package.IsLastPackage)
        {
            await _readyForNextChunkSubject.OnCompletedAsync();
        }
        else
        {
            await _readyForNextChunkSubject.OnNextAsync(new PreReadPackage(remainedBytes,
                remainingBytesLength));
        }
    }
      
    public async ValueTask OnNextAsync(ReadingPhasePackage package)
    {
        await ExtractNext(package);
    }

    public ValueTask OnErrorAsync(Exception error)
    {
        return _readyForNextChunkSubject.OnCompletedAsync();
    }

    public ValueTask OnCompletedAsync()
    {
        //we will complete this sequence as well, in such case there is nothing to do. Something went wrong
        return _readyForNextChunkSubject.OnCompletedAsync();
    }
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
}