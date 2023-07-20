using System.Buffers;
using System.Reactive.Subjects;

namespace SortingEngine.RowData;

public sealed class ObservableLinesExtractor 
{
    private readonly SimpleAsyncSubject<PreReadPackage> _readyForNextChunkSubject =
        new SequentialSimpleAsyncSubject<PreReadPackage>();
      
    private readonly LinesExtractor _linesExtractor;

    public ObservableLinesExtractor(byte[] eol, byte[] lineDelimiter)
    {
        _linesExtractor = new LinesExtractor(eol, lineDelimiter);
    }

    public async Task<(SortingPhasePackage,PreReadPackage)> ExtractNextPartAsync(ReadingPhasePackage package)
    {
        ExtractionResult result = _linesExtractor.ExtractRecords(package.RowData.AsSpan()[..package.WrittenBytesLength],
            package.ParsedRecords);

        if (!result.Success)
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));

        int remainingBytesLength = package.WrittenBytesLength - result.StartRemainingBytes;
        
        //will be returned in SortingPhasePoolManager
        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.RowData.AsSpan()[result.StartRemainingBytes..package.WrittenBytesLength].CopyTo(remainedBytes);

        SortingPhasePackage nextPackage = new SortingPhasePackage(package.RowData, package.WrittenBytesLength,
            package.ParsedRecords, result.LinesNumber, package.PackageNumber, package.IsLastPackage);

        PreReadPackage preReadPackage = package.IsLastPackage
            ? PreReadPackage.LastPackage(package.PackageNumber)
            : new PreReadPackage(remainedBytes,
                remainingBytesLength, package.PackageNumber, false);
        
        return (nextPackage, preReadPackage);
    }
}