using System.Buffers;
using System.Reactive.Subjects;
using System.Text;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;

namespace SortingEngine.RowData;

public sealed class ObservableLinesExtractor
{
    private readonly SimpleAsyncSubject<PreReadPackage> _readyForNextChunkSubject =
        new SequentialSimpleAsyncSubject<PreReadPackage>();

    private readonly LinesExtractor _linesExtractor;

    public ObservableLinesExtractor(LinesExtractor linesExtractor)
    {
        _linesExtractor = Guard.NotNull(linesExtractor);
    }

    public async Task<(SortingPhasePackage, PreReadPackage)> ExtractNextPartAsync(ReadyForExtractionPackage package)
    {
        ExtractionResult result = _linesExtractor.ExtractRecords(package.LineData.Span, package.ParsedRecords);

        if (!result.Success)
        {
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
            throw new InvalidOperationException(result.Message);
        }

        int remainingBytesLength = package.LineData.Length - result.StartRemainingBytes;

        //will be returned in SortingPhasePoolManager
        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.LineData[result.StartRemainingBytes..].CopyTo(remainedBytes);

        SortingPhasePackage nextPackage = new SortingPhasePackage(package, result.LinesNumber);

        PreReadPackage preReadPackage = package.IsLastPackage
            ? PreReadPackage.LastPackage(package.Id)
            : new PreReadPackage(package.Id, false, remainedBytes, remainingBytesLength);

        return (nextPackage, preReadPackage);
    }
}