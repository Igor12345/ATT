//Remove the comment or add the symbol to the solution,
//and the merge phase will slow down by almost 30%.

// #define MERGE_ASYNC
using System.Diagnostics;
using System.Reactive.Linq;
using ConsoleWrapper.IOProcessing;
using Infrastructure.MemoryTools;
using LogsHub;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SortingEngine;
using SortingEngine.Merging;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.RuntimeEnvironment;
using SortingEngine.Sorting;

namespace ConsoleWrapper;

internal class FileSortingService : IHostedService
{
   private readonly BaseConfiguration _baseOptions;
   private readonly InputParameters _input;

   public FileSortingService(IOptions<BaseConfiguration> baseOptions, IOptions<InputParameters> input)
   {
      _baseOptions = baseOptions.Value;
      _input = input.Value;
   }

   public async Task StartAsync(CancellationToken cancellationToken)
   {
      Stopwatch sw = Stopwatch.StartNew();
      
      Result finalResult = await Execute(cancellationToken);
      
      sw.Stop();
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine(finalResult.Success
         ? $"---> Success - {sw.Elapsed.TotalMinutes:F2} min, {sw.Elapsed.Seconds:F2} sec; " +
           $"Total execution time: {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms"
         : $"---> Error: {finalResult.Message}");

      Console.ReadLine();
   }
   public async Task StopAsync(CancellationToken cancellationToken)
   {
      Console.WriteLine("******* Final ********");
      await Task.Delay(2);
   }

   private async Task<Result> Execute(CancellationToken cancellationToken)
   {
      InputParametersValidator inputParametersValidator = new InputParametersValidator();
      (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

      if (!canContinue)
         return Result.Error("Error");

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer(_baseOptions);
      IConfig configuration = analyzer.SuggestConfig(validInput);

      //only for demonstration, use NLog, Serilog, ... in real projects
      // https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
      // https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
      // ILogger logger = Logger.Create(cancellationToken);
      ILogger logger = Logger.CreateEmpty(cancellationToken);

      Console.WriteLine($"Starting at: {DateTime.UtcNow:hh:mm:ss-fff}, sorting file: {validInput.File}.");

      SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

      //todo return result
      Result sortingResult = await SortingPhase(configuration, validInput, semaphore, logger, cancellationToken);
      if (!sortingResult.Success)
      {
         Console.WriteLine($"Sorting phase completed with an error {sortingResult.Message}");
         return sortingResult;
      }
      
      Console.WriteLine($"Sorting phase completed at: {DateTime.UtcNow:hh:mm:ss-fff}.");

      Result result = configuration.UseOneWay
         ? Result.Ok

#if MERGE_ASYNC
         : await MergingPhaseAsync(configuration, logger);
#else
         :  MergingPhase(configuration, logger);
#endif
      
      Console.WriteLine($"Completed at: {DateTime.UtcNow:hh:mm:ss-fff}, the file: {configuration.Output}.");

      return result;
   }

   private async Task<Result> SortingPhase(IConfig configuration,
      ValidatedInputParameters validInput, SemaphoreSlim semaphore, ILogger logger, CancellationToken cancellationToken)
   {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      
      ObservableRecordsExtractor extractor = new ObservableRecordsExtractor(
         configuration.Encoding.GetBytes(Environment.NewLine),
         configuration.Encoding.GetBytes(Constants.Delimiter), logger, cancellationToken);

      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration, logger, cancellationToken);

      IBytesProducer bytesReader =
         new LongFileReader(validInput.File, configuration.Encoding, configuration.ReadStreamBufferSize, logger,
            cancellationToken);
      
      SetOfLinesSorter sorter = new SetOfLinesSorter(logger, buffer => new LinesSorter(buffer));
      
      using SortingPhasePool sortingPhasePool = new SortingPhasePool(configuration.SortingPhaseConcurrency,
         configuration.InputBufferLength,
         configuration.RecordsBufferLength, logger);
      
      using SortingPhasePoolAsObserver sortingPhasePoolAsObserver = new SortingPhasePoolAsObserver(sortingPhasePool, logger);

      await using IAsyncDisposable? releaseBufferSub =
         await chunksDirector.SortedLinesSaved.SubscribeAsync(sortingPhasePoolAsObserver.ReleaseBuffers);

      var published = sortingPhasePoolAsObserver.StreamLinesByBatches(cancellationToken)
         .Select(async p => await bytesReader.ProcessPackageAsync(p))
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
            p =>
            {},
            ex =>
            {
               //Here can be some smarter handler
               HandleError(ex);
               throw ex;
            },
            () =>
            {
               bytesReader.Dispose();
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

   private static async Task<Result> MergingPhaseAsync(IConfig configuration,
      ILogger logger)
   {
      using ILinesWriter resultWriter =
         LinesWriter.Create(configuration.Output, configuration.Encoding.GetBytes(".").Length,
            configuration.WriteStreamBufferSize, logger);

      Console.WriteLine("The merge phase runs asynchronously.");
      Stopwatch sw = new Stopwatch();
      sw.Start();
      StreamsMergeExecutorAsync mergerAsync = new StreamsMergeExecutorAsync(configuration, resultWriter);
      Result result = await mergerAsync.MergeWithOrderAsync();
      sw.Stop();
      Console.WriteLine($"Merge completed in {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms");
      
      return result;
   }

   private static Result MergingPhase(IConfig configuration,
      ILogger logger)
   {
      using ILinesWriter resultWriter =
         LinesWriter.Create(configuration.Output, configuration.Encoding.GetBytes(".").Length,
            configuration.WriteStreamBufferSize, logger);

      Console.WriteLine("The merge phase is executed in synchronous mode.");
      Stopwatch sw = new Stopwatch();
      sw.Start();
      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration, resultWriter);
      Result result = merger.MergeWithOrder();
      sw.Stop();
      Console.WriteLine($"Merge completed in {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms");
      
      return result;
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