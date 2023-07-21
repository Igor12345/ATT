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

    public async Task<(SortingPhasePackage,PreReadPackage)> ExtractNextPartAsync(ReadyForExtractionPackage package)
    {
        ExtractionResult result = _linesExtractor.ExtractRecords(package.LineData.Span, package.ParsedRecords);

        if (!result.Success)
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));

        int remainingBytesLength = package.WrittenBytesLength - result.StartRemainingBytes;
        
        //will be returned in SortingPhasePoolManager
        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.LineData[result.StartRemainingBytes..].CopyTo(remainedBytes);

        SortingPhasePackage nextPackage = new SortingPhasePackage(package.RowData, package.LineData, package.WrittenBytesLength,
            package.ParsedRecords, result.LinesNumber, package.Id, package.IsLastPackage);

        PreReadPackage preReadPackage = package.IsLastPackage
            ? PreReadPackage.LastPackage(package.Id)
            : new PreReadPackage(remainedBytes,
                remainingBytesLength, package.Id, false);
        
        return (nextPackage, preReadPackage);
    }
}