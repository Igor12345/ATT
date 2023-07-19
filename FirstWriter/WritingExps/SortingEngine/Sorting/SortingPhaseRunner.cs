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
    private readonly ILinesWriter _linesWriter;
    private readonly IBytesProducer _bytesProducer;

    public SortingPhaseRunner(IBytesProducer bytesProducer, ILinesWriter linesWriter)
    {
        _bytesProducer = Guard.NotNull(bytesProducer);
        _linesWriter = Guard.NotNull(linesWriter);
    }
    
    public async Task<Result> Execute(IConfig configuration, SemaphoreSlim semaphore, ILogger logger, CancellationToken cancellationToken)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
      
        ObservableRecordsExtractor extractor = new ObservableRecordsExtractor(
            configuration.Encoding.GetBytes(Environment.NewLine),
            configuration.Encoding.GetBytes(Constants.Delimiter), logger, cancellationToken);

        IntermediateResultsDirector chunksDirector =
            IntermediateResultsDirector.Create(_linesWriter, configuration, logger, cancellationToken);
      
        SetOfLinesSorter sorter = new SetOfLinesSorter(logger, buffer => new LinesSorter(buffer));
      
        using SortingPhasePool sortingPhasePool = new SortingPhasePool(configuration.SortingPhaseConcurrency,
            configuration.InputBufferLength,
            configuration.RecordsBufferLength, logger);
      
        using SortingPhasePoolAsObserver sortingPhasePoolAsObserver = new SortingPhasePoolAsObserver(sortingPhasePool, logger);

        await using IAsyncDisposable? releaseBufferSub =
            await chunksDirector.SortedLinesSaved.SubscribeAsync(sortingPhasePoolAsObserver.ReleaseBuffers);

        var published = sortingPhasePoolAsObserver.StreamLinesByBatches(cancellationToken)
            .Select(async p => await _bytesProducer.ProcessPackageAsync(p))
            .Select(async p => await extractor.ExtractNextAsync(p))
            .Publish();

        await using var backLoopSub = await published.Select(pp => pp.Item2)
            .SubscribeAsync(sortingPhasePoolAsObserver.ReadyProcessingNextChunk);

        await using var sortingSub = await published
            .Select(pp => pp.Item1)
            .Select(p => AsyncObservable.FromAsync(async () => await sorter.ProcessPackageAsync(p)))
            .Merge()
            .Select(async p => await chunksDirector.ProcessPackageAsync(p))
            .SubscribeAsync(
                _ =>
                {},
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
        await sortingPhasePoolAsObserver.LetsStartAsync();
         
        await semaphore.WaitAsync(cancellationToken);
      
        sw.Stop();
        Console.WriteLine($"The sorting phase completed in {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms");
      
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