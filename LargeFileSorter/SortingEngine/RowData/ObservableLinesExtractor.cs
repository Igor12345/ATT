using System.Buffers;
using System.Reactive.Subjects;
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

    //todo split on parser
    public async Task<(SortingPhasePackage, PreReadPackage)> ExtractNextPartAsync(ReadyForExtractionPackage package)
    {
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) ObservableLinesExtractor Processing {package.Id} is last {package.IsLastPackage}");

        ExtractionResult result = _linesExtractor.ExtractRecords(package.LineData.Span, package.ParsedRecords);

        if (!result.Success)
        {
            Console.WriteLine(
                $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) !!!! ObservableLinesExtractor " +
                $"Processed {package.Id} extracted with error {result.Message}");
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
            throw new InvalidOperationException(result.Message);
        }

        int remainingBytesLength = package.LineData.Length - result.StartRemainingBytes;

        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) ObservableLinesExtractor " +
            $"Processed {package.Id} extracted {result.LinesNumber} lines, left {remainingBytesLength} bytes");

        //will be returned in SortingPhasePoolManager
        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.LineData[result.StartRemainingBytes..].CopyTo(remainedBytes);

        //todo delete
        var l = ByteToStringConverter.Convert(remainedBytes);
        Console.WriteLine($"----> Remained bytes after package {package.Id} are: {l}");
        
        SortingPhasePackage nextPackage = new SortingPhasePackage(package, result.LinesNumber);

        PreReadPackage preReadPackage = package.IsLastPackage
            ? PreReadPackage.LastPackage(package.Id)
            : new PreReadPackage(package.Id, false, remainedBytes, remainingBytesLength);

        return (nextPackage, preReadPackage);
    }
}