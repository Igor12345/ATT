using System.Diagnostics;
using System.Reactive.Linq;
using Infrastructure.MemoryTools;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.Sorting;

public class SortingPhaseRunner
{
    private readonly IOneTimeLinesWriter _linesWriter;
    private readonly IBytesProducer _bytesProducer;

    public SortingPhaseRunner(IBytesProducer bytesProducer, IOneTimeLinesWriter linesWriter)
    {
        _bytesProducer = Guard.NotNull(bytesProducer);
        _linesWriter = Guard.NotNull(linesWriter);
    }

    public async Task<Result> Execute(IConfig configuration, SemaphoreSlim semaphore, ILogger logger,
        CancellationToken cancellationToken)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        ObservableLinesExtractor extractor = new ObservableLinesExtractor(
            configuration.EolBytes, configuration.DelimiterBytes);

        IntermediateResultsDirector chunksDirector = IntermediateResultsDirector.Create(_linesWriter, configuration);

        SetOfLinesSorter sorter = new SetOfLinesSorter(logger, buffer => new LinesSorter(buffer));

        using SortingPhasePool sortingPhasePool = new SortingPhasePool(configuration.SortingPhaseConcurrency,
            configuration.InputBufferLength,
            configuration.RecordsBufferLength, _bytesProducer);

        using SortingPhasePoolAsObserver sortingPhasePoolAsObserver = new SortingPhasePoolAsObserver(sortingPhasePool, configuration.MaxLineLength);

        await using IAsyncDisposable? releaseBufferSub =
            await chunksDirector.SortedLinesSaved
                
                .Do(async p => await logger.LogAsync(() =>
                    $"All lines in the package: {p.Id} has been saved in a file, " +
                    $"and the buffer is ready to reuse. This is the last part: {p.IsLastPackage}."))
                
                .SubscribeAsync(sortingPhasePoolAsObserver.ReleaseBuffers);

        var published = 
            sortingPhasePoolAsObserver.StreamLinesByBatches(cancellationToken)
            
            .Do(async p => await logger.LogAsync(() =>
                new LogEntry($"Ready to read the next chunk of data. package: {p.Id}.")))
            
            // .Select(async p => await _bytesProducer.WriteBytesToBufferAsync(p))
            //
            // .Do(async p => await logger.LogAsync(() =>
            //     new LogEntry(
            //         $"The next chunk of data has been read. package: {p.Id}, " +
            //         $"contains: {p.WrittenBytesLength} bytes, this is the last part: {p.IsLastPackage}.")))
            
            .Select(async p => await extractor.ExtractNextPartAsync(p))
            .Publish();

        await using var backLoopSub = await published.Select(pp => pp.Item2)
            
            .Do(async p => await logger.LogAsync(() =>
                $"The package: {p.Id} left a tail of {p.RemainedBytesLength} bytes; "))
            
            .SubscribeAsync(sortingPhasePoolAsObserver.ReadyProcessingNextChunk);

        await using var sortingSub = await published
            
            .Select(pp => pp.Item1)
            
            .Do(async p => await logger.LogAsync(() =>
                $"{p.LinesNumber} lines were extracted in the package: {p.Id}, " +
                $"this is the last part: {p.IsLastPackage}."))
            
            .Select(p => AsyncObservable.FromAsync(async () => await sorter.ProcessPackageAsync(p)))
            .Merge()
            
            .Do(async p => await logger.LogAsync(() =>
                $"Were lines in the package: {p.Id} has been sorted, this is the last part: {p.IsLastPackage}."))
            
            .Select(async p => await chunksDirector.ProcessPackageAsync(p))
            
            .SubscribeAsync(
                _ => { },
                ex =>
                {
                    //Here can be some smarter handler
                    HandleError(ex);
                    throw ex;
                },
                () =>
                {
                    _bytesProducer.Dispose();
                    MemoryCleaner.CleanMemory();
                    semaphore.Release();
                }
            );
        await published.ConnectAsync();
        await sortingPhasePoolAsObserver.LetsStartAsync(cancellationToken);

        await semaphore.WaitAsync(cancellationToken);

        sw.Stop();
        Console.WriteLine(
            $"---> The sorting phase has completed in {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms");

        return Result.Ok;
    }

    private void HandleError(Exception exception)
    {
        var color = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(exception);
        }
        finally
        {
            Console.ForegroundColor = color;
        }
    }
}