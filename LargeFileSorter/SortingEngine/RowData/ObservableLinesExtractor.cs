using System.Buffers;
using Infrastructure.Parameters;

namespace SortingEngine.RowData;

public sealed class ObservableLinesExtractor
{
    private readonly LinesExtractor _linesExtractor;

    public ObservableLinesExtractor(LinesExtractor linesExtractor)
    {
        _linesExtractor = Guard.NotNull(linesExtractor);
    }

    public (SortingPhasePackage, PreReadPackage) ExtractNextPart(ReadyForExtractionPackage package)
    {
        ExtractionResult result = _linesExtractor.ExtractRecords(package.LineData.Span, package.ParsedRecords);

        if (!result.Success)
        {
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