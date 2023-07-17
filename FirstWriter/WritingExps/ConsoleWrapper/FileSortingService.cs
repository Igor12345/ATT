using System.Diagnostics;
using System.Reactive.Linq;
using ConsoleWrapper.IOProcessing;
using Infrastructure.MemoryTools;
using LogsHub;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SortingEngine;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.RuntimeEnvironment;

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
      Stopwatch sw1 = Stopwatch.StartNew();
      
      var final = await ViaIObservable(cancellationToken);
      
      sw1.Stop();
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine(final.Success
         ? $"---> Success - {sw1.Elapsed.TotalMinutes} min, {sw1.Elapsed.Seconds} sec; " +
           $"Total: {sw1.Elapsed.TotalSeconds} sec, {sw1.Elapsed.TotalMilliseconds} ms"
         : $"---> Error: {final.Message}");

      Console.ReadLine();
      
      return;

      await FirstHardCodedApproach(cancellationToken);
   }
   public async Task StopAsync(CancellationToken cancellationToken)
   {
      //todo
      Console.WriteLine("Bye service");
      await Task.Delay(2);
   }

   private async Task<Result> ViaIObservable(CancellationToken cancellationToken)
   {
      InputParametersValidator inputParametersValidator = new InputParametersValidator();
      (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

      if (!canContinue)
         return Result.Error("Error");

      Console.WriteLine("--> Ready to start");
      var r = Console.ReadLine();

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer(_baseOptions);
      IConfig configuration = analyzer.SuggestConfig(validInput);

      //only for demonstration, use NLog, Serilog, ... in real projects
      // https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
      // https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
      ILogger logger = Logger.Create(cancellationToken);
      // ILogger logger = Logger.CreateEmpty(cancellationToken);

      // await logger.LogAsync($"Started at {DateTime.UtcNow:s}");

      SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

      await SortingPhase(cancellationToken, configuration, validInput, semaphore, logger);

      Console.WriteLine("After sorting phase");

      Result result = await MergingPhase(cancellationToken, configuration, logger);
      
      Console.WriteLine("******* Final ********");

      return result;
   }

   private static async Task<Result> MergingPhase(CancellationToken cancellationToken, IConfig configuration,
      ILogger logger)
   {
      using ILinesWriter resultWriter =
         RecordsWriter.Create(configuration.Output, configuration.Encoding.GetBytes(".").Length, logger);
      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration, resultWriter);

      var result = await merger.MergeWithOrder();
      return result;
   }

   private async Task SortingPhase(CancellationToken cancellationToken, IConfig configuration,
      ValidatedInputParameters validInput, SemaphoreSlim semaphore, ILogger logger)
   {
      ObservableRecordsExtractor extractor = new ObservableRecordsExtractor(
         configuration.Encoding.GetBytes(Environment.NewLine),
         configuration.Encoding.GetBytes(Constants.Delimiter), logger, cancellationToken);

      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration, logger, cancellationToken);

      //todo!!!
      // await using 
      await using IBytesProducer bytesReader =
         new LongFileReader(validInput.File, configuration.Encoding, logger, cancellationToken);
      BunchOfLinesSorter sorter = new BunchOfLinesSorter(logger);

      using SortingPhasePool sortingPhasePool = new SortingPhasePool(configuration.SortingPhaseConcurrency,
         configuration.InputBufferLength,
         configuration.RecordsBufferLength, logger);
      
      using SortingPhasePoolAsObserver sortingPhasePoolAsObserver = new SortingPhasePoolAsObserver(sortingPhasePool, logger);

      // await using var s1 = await sortingPhasePoolManager.LoadNextChunk.SubscribeAsync(bytesReader);
      
      // await using var s4 = await extractor.ReadyForNextChunk.SubscribeAsync(sortingPhasePoolManager);
      await using var s6 =
         await chunksDirector.SortedLinesSaved.SubscribeAsync(sortingPhasePoolAsObserver.ReleaseBuffers);

      var published = sortingPhasePoolAsObserver.StreamLinesByBatches(cancellationToken)
         // .Do(p => Console.WriteLine(
         //    $"<<-->> LoadNextChunk sequence for {p.PackageNumber}, step 1, last: {p.IsLastPackage}, bufferId: {p.RowData.GetHashCode()}"))
         .Select(async p => await bytesReader.ProcessPackage(p))
         // .Do(p=> Console.WriteLine($"<<-->> LoadNextChunk sequence for {p.PackageNumber}, step 2, last: {p.IsLastPackage}, bufferId: {p.RowData.GetHashCode()}"))
         .Select(async p => await extractor.ExtractNext(p))

         // .Do(p=> Console.WriteLine($"<<-->> LoadNextChunk sequence for {p.Item1.PackageNumber}, step 3.1 forward to sorting, last: {p.Item1.IsLastPackage}, bufferId: {p.Item1.RowData.GetHashCode()}"))
         // .Do(p=> Console.WriteLine($"<<-->> LoadNextChunk sequence for {p.Item2.PackageNumber}, step 3.2 Back loop, last: {p.Item2.IsLastPackage}"))
         .Publish();

      await using var backSeq = await published.Select(pp => pp.Item2)
         .SubscribeAsync(sortingPhasePoolAsObserver.ReadyProcessingNextChunk);

      await using var sortingSeq = await published
         .Select(pp => pp.Item1)
         .Select(p => AsyncObservable.FromAsync(async () => await sorter.ProcessPackage(p)))
         .Merge()
         // .Do(p=> Console.WriteLine($"<<++>> Sorting sequence for {p.PackageNumber}, step 1, last: {p.IsLastPackage}, bufferId: {p.RowData.GetHashCode()}, sorted: {p.LinesNumber}"))
         .Select(async p => await chunksDirector.ProcessPackage(p))
         // .Do(p=> Console.WriteLine($"<<++>> Sorting sequence for {p.PackageNumber}, step 2, last: {p.IsLastPackage}, bufferId: {p.RowData.GetHashCode()}, saved: {p.LinesNumber}"))
         .SubscribeAsync(
            p =>
            {
               // Console.WriteLine($"Final subscription!!! sortingPhasePoolManager.LoadNextChunk processed package: {p.PackageNumber}");
            },
            ex =>
            {
               //Here can be some smarter handler
               HandleError(ex);
               throw ex;
            },
            () =>
            {
               Console.WriteLine("--->  Before GC");
               
               MemoryCleaner.CleanMemory();
               Console.WriteLine("<---  After GC");

               semaphore.Release();
            }
         );

      await published.ConnectAsync();

      await sortingPhasePoolAsObserver.LetsStart();
         
      await semaphore.WaitAsync(cancellationToken);
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

   private async Task FirstHardCodedApproach(CancellationToken cancellationToken)
   {
      InputParametersValidator inputParametersValidator = new InputParametersValidator();
      (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

      if (!canContinue)
         return;

      Console.WriteLine("--> Ready to start");
      var r = Console.ReadLine();

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer(_baseOptions);
      IConfig configuration = analyzer.SuggestConfig(validInput);

      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration);
      Logger logger = Logger.Create(cancellationToken);
      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration, logger, cancellationToken);
      await using ResultWriter resultWriter = ResultWriter.Create(configuration.Output, logger, cancellationToken);

      merger.OutputBufferFull += (o, eventArgs) =>
      {
         Console.WriteLine("Writing merge result");
         resultWriter.WriteOutput(eventArgs);
      };

      Stopwatch sw = Stopwatch.StartNew();

      await using (IBytesProducer bytesReader =
                   new LongFileReader(validInput.File, validInput.Encoding, logger, cancellationToken))
      {
         RecordsSetSorter sorter = new RecordsSetSorter(configuration);
         // sorter.SortingCompleted += (o, eventArgs) =>
         // {
         //    Console.WriteLine("Writing chunk");
         //    chunksDirector.WriteRecords(eventArgs);
         // };
         // sorter.CheckPoint += (o, eventArgs) =>
         // {
         //    Console.WriteLine($"--->  {eventArgs.Name} check point");
         //    Console.ReadLine();
         //    Console.WriteLine(" <---");
         // };

         Console.WriteLine("Before starting");

         Result result = await sorter.SortAsync(bytesReader, cancellationToken);

         long memory = GC.GetTotalMemory(false);
         Console.WriteLine($"Memory before GC {memory}");
         MemoryCleaner.CleanMemory();
         memory = GC.GetTotalMemory(false);
         Console.WriteLine($"Memory after GC {memory}");
      }

      var memory2 = GC.GetTotalMemory(false);
      Console.WriteLine($"Memory before merge GC {memory2}");
      var res = await merger.MergeWithOrder();

      sw.Stop();
      Console.WriteLine(res.Success
         ? $"---> Success - {sw.Elapsed.TotalMinutes} min, {sw.Elapsed.Seconds} sec; Total: {sw.Elapsed.TotalSeconds} sec, {sw.Elapsed.TotalMilliseconds} ms"
         : $"---> Error: {res.Message}");

      await Task.Delay(2);
   }

}