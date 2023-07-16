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
      ILogger logger = Logger.Create(cancellationToken);
      // ILogger logger = Logger.CreateEmpty(cancellationToken);

      // await logger.LogAsync($"Started at {DateTime.UtcNow:s}");

      SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

      await SortingPhase(cancellationToken, configuration, validInput, semaphore, logger);

      Console.WriteLine("After sorting phase");


      Console.WriteLine($"----->  Awaiting semaphore, thread {Thread.CurrentThread.ManagedThreadId}");
      await semaphore.WaitAsync(cancellationToken);
      Console.WriteLine($"<---- Semaphore passed thread {Thread.CurrentThread.ManagedThreadId}");
      var result = await MergingPhase(cancellationToken, configuration, logger);
      
      Console.WriteLine("******* Final ********");

      return result;
   }

   private static async Task<Result> MergingPhase(CancellationToken cancellationToken, IConfig configuration,
      ILogger logger)
   {
      // Console.WriteLine("_____ Before merging");
      using ILinesWriter resultWriter =
         RecordsWriter.Create(configuration.Output, configuration.Encoding.GetBytes(".").Length, logger);
      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration, resultWriter);

      var result = await merger.MergeWithOrder();

      // Console.WriteLine("--- After merging");
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
         IBytesProducer bytesReader =
         new LongFileReader(validInput.File, configuration.Encoding, logger, cancellationToken);
      BunchOfLinesSorter sorter = new BunchOfLinesSorter(logger);

      using SortingPhasePoolManager sortingPhasePoolManager = new SortingPhasePoolManager(3,
         configuration.InputBufferLength,
         configuration.RecordsBufferLength, logger, cancellationToken);

      // await using var s1 = await sortingPhasePoolManager.LoadNextChunk.SubscribeAsync(bytesReader);
      
      // await using var s4 = await extractor.ReadyForNextChunk.SubscribeAsync(sortingPhasePoolManager);
      await using var s6 = await chunksDirector.SortedLinesSaved.SubscribeAsync(sortingPhasePoolManager);

      var published = sortingPhasePoolManager.LoadNextChunk
         .Select(async p => await bytesReader.ProcessPackage(p))
         .Select(async p => await extractor.ExtractNext(p))
         .Publish();

      var backSeq = await published.Select(pp => pp.Item2)
         .Select(async p => await sortingPhasePoolManager.OnNextAsync(p))
         .SubscribeAsync(
            (p) =>
            {
               Console.WriteLine($"OnNext in subscription ReadyForNextChunk");
            },
            ex =>
            {
               //todo
            },
            () => { 
               Console.WriteLine($"OnCompleted in subscription ReadyForNextChunk");}
         );

      var sortingSeq = await published
         .Select(pp => pp.Item1)
         .Select(p => AsyncObservable.FromAsync(async () => await sorter.ProcessPackage(p)))
         .Merge()
         .Select(async p => await chunksDirector.ProcessPackage(p))
         .SubscribeAsync(
            p =>
            {
               Console.WriteLine($"sortingPhasePoolManager.LoadNextChunk processed package: {p.PackageNumber}");
            },
            ex =>
            {
               semaphore.Release();
            },
            () =>
            {
               Console.WriteLine($"sortingPhasePoolManager.LoadNextChunk completed");

               Console.WriteLine("--->  Before GC");
               MemoryCleaner.CleanMemory();
               Console.WriteLine("<---  After GC");

               Console.WriteLine("Releasing semaphore");
               semaphore.Release();
               Console.WriteLine("Semaphore released");
               StartMerge();
            }
         );

      await published.ConnectAsync();

      Console.WriteLine("All subscriptions ready");
      await sortingPhasePoolManager.LetsStart();
         
      // await using var s2 = await bytesReader.NextChunkPrepared.SubscribeAsync(extractor);
      // await using var s3 = await extractor.ReadyForSorting.SubscribeAsync(sorter);
      // await using var s5 = await sorter.SortingCompleted.SubscribeAsync(chunksDirector);

      // await using var s7 = await chunksDirector.SortedLinesSaved.SubscribeAsync(
      //    p =>
      //    {
      //       Console.WriteLine(
      //          $"--> Process package {p.PackageNumber} on thread: {Thread.CurrentThread.ManagedThreadId} ");
      //    },
      //    e => HandleError(e),
      //    () =>
      //    {
      //       Console.WriteLine("--->  Before GC");
      //       MemoryCleaner.CleanMemory();
      //       Console.WriteLine("<---  After GC");
      //       
      //       Console.WriteLine("Releasing semaphore");
      //       semaphore.Release();
      //       Console.WriteLine("Semaphore released");
      //       StartMerge();
      //    }
      // );

      await sortingPhasePoolManager.LetsStart();
   }

   private void HandleError(Exception exception)
   {
      var color = Console.ForegroundColor;
      try
      {
         Console.ForegroundColor = ConsoleColor.DarkRed;
         Console.WriteLine(exception.Message);
         Console.WriteLine(exception.ToString());
         Console.WriteLine(exception.StackTrace);
      }
      finally
      {
         Console.ForegroundColor = color;
      }
      
   }

   private void StartMerge()
   {
      Console.WriteLine("---> Ready for merge! <---");
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
         sorter.SortingCompleted += (o, eventArgs) =>
         {
            Console.WriteLine("Writing chunk");
            chunksDirector.WriteRecords(eventArgs);
         };
         sorter.CheckPoint += (o, eventArgs) =>
         {
            Console.WriteLine($"--->  {eventArgs.Name} check point");
            Console.ReadLine();
            Console.WriteLine(" <---");
         };

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