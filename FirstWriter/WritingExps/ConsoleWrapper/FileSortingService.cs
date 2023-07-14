﻿using System.Diagnostics;
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
      await ViaIObservable(cancellationToken);
      return;

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
         IntermediateResultsDirector.Create(configuration.TemporaryFolder, logger, cancellationToken);
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

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      //todo
      Console.WriteLine("Bye service");
      await Task.Delay(2);
   }

   private async Task ViaIObservable(CancellationToken cancellationToken)
   {
      InputParametersValidator inputParametersValidator = new InputParametersValidator();
      (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

      if (!canContinue)
         return;

      Console.WriteLine("--> Ready to start");
      var r = Console.ReadLine();

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer(_baseOptions);
      IConfig configuration = analyzer.SuggestConfig(validInput);

      //only for demonstration, use NLog, Serilog, ... in real projects
      Logger logger = Logger.Create(cancellationToken);

      await logger.LogAsync($"Started at {DateTime.UtcNow:s}");

      SortingPhase(cancellationToken, configuration, logger, validInput).GetAwaiter().GetResult();

      Console.WriteLine("After sorting phase");
      Console.ReadLine();
      await MergingPhase(cancellationToken, configuration, logger);
      
      Console.WriteLine("******* Final ********");
      Console.ReadLine();
   }

   private static async Task MergingPhase(CancellationToken cancellationToken, IConfig configuration, Logger logger)
   {
      Console.WriteLine("_____ Before merging");
      await using ResultWriter resultWriter = ResultWriter.Create(configuration.Output, logger, cancellationToken);
      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration);
      Console.WriteLine("--- Last line");
   }

   private async Task SortingPhase(CancellationToken cancellationToken, IConfig configuration, Logger logger,
      ValidatedInputParameters validInput)
   {
      RecordsExtractorAsSequence extractor = new RecordsExtractorAsSequence(
         configuration.Encoding.GetBytes(Environment.NewLine),
         configuration.Encoding.GetBytes(". "), logger, cancellationToken);

      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration.TemporaryFolder, logger, cancellationToken);

      await using IBytesProducer bytesReader =
         new LongFileReader(validInput.File, validInput.Encoding, logger, cancellationToken);
      LinesSorter sorter = new LinesSorter(logger);

      using SortingPhasePoolManager sortingPhasePoolManager = new SortingPhasePoolManager(3,
         configuration.InputBufferLength,
         configuration.RecordsBufferLength, logger, cancellationToken);

      await using var s1 = await sortingPhasePoolManager.LoadNextChunk.SubscribeAsync(bytesReader);
      await using var s2 = await bytesReader.NextChunkPrepared.SubscribeAsync(extractor);

      await using var s3 = await extractor.ReadyForSorting.SubscribeAsync(sorter);
      await using var s4 = await extractor.ReadyForNextChunk.SubscribeAsync(sortingPhasePoolManager);
      await using var s5 = await sorter.SortingCompleted.SubscribeAsync(chunksDirector);
      await using var s6 = await chunksDirector.SortedLinesSaved.SubscribeSafeAsync(sortingPhasePoolManager);

      await using var s7 = await chunksDirector.SortedLinesSaved.SubscribeAsync(
         b => { },
         e => HandleError(e),
         () =>
         {
            Console.WriteLine("--->  Before GC");
            Console.ReadLine();
            MemoryCleaner.CleanMemory();
            Console.WriteLine("<---  After GC");
            Console.ReadLine();
            StartMerge();
         }
      );

      await sortingPhasePoolManager.LetsStart();
   }

   private void HandleError(Exception exception)
   {
   }

   private void StartMerge()
   {
      Console.WriteLine("---> Ready for merge! <---");
   }
}