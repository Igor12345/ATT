//Remove the comment or add the symbol to the solution,
//and the merge phase will slow down by almost 30%.

// #define MERGE_ASYNC

using System.Diagnostics;
using System.Runtime;
using ConsoleWrapper.IOProcessing;
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
      CheckGCMode();

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
      await Task.Delay(2, cancellationToken);
   }

   private static void CheckGCMode()
   {
      var result = GCSettings.IsServerGC ? "server" : "workstation";
      Console.WriteLine("The {0} garbage collector is running.", result);
   }

   private async Task<Result> Execute(CancellationToken cancellationToken)
   {
      InputParametersValidator inputParametersValidator = new InputParametersValidator();
      (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

      if (!canContinue)
         return Result.Error("Something is wrong with the configuration.");

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer(_baseOptions);
      IConfig configuration = analyzer.SuggestConfig(validInput);

      //only for demonstration, use NLog, Serilog, ... in real projects
      // https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
      // https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
      ILogger logger = Logger.Create(cancellationToken);
      // ILogger logger = Logger.CreateEmpty(cancellationToken);

      Console.WriteLine($"Starting at: {DateTime.UtcNow:hh:mm:ss-fff}, sorting file: {validInput.File}.");

      SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

      IBytesProducer bytesReader = configuration.KeepReadStreamOpen
         ? new LongFileReader(validInput.File, configuration.Encoding, configuration.ReadStreamBufferSize, logger,
            cancellationToken)
         : new LongFileReaderKeepStream(validInput.File, configuration.Encoding, configuration.ReadStreamBufferSize,
            logger,
            cancellationToken);
      
      IOneTimeLinesWriter writer = LinesWriter.CreateForOnceWriting(configuration.Encoding.GetBytes("1").Length, configuration.ReadStreamBufferSize);
      SortingPhaseRunner sortingPhase = new SortingPhaseRunner(bytesReader, writer);
      
      Result sortingResult = await sortingPhase.Execute(configuration, semaphore, logger, cancellationToken);
      if (!sortingResult.Success)
      {
         Console.WriteLine($"Sorting phase completed with an error {sortingResult.Message}");
         return sortingResult;
      }
      
      Console.WriteLine($"Sorting phase completed at: {DateTime.UtcNow:hh:mm:ss-fff}.");

      Result result = configuration.UseOneWay
         ? Result.Ok
#if MERGE_ASYNC
         : await MergingPhaseAsync(configuration);
#else
         :  MergingPhase(configuration);
#endif
      
      Console.WriteLine($"Completed at: {DateTime.UtcNow:hh:mm:ss-fff}, the file: {configuration.Output}.");

      return result;
   }
   
   private static async Task<Result> MergingPhaseAsync(IConfig configuration)
   {
      ISeveralTimesLinesWriter resultWriter = LinesWriter.CreateForMultipleWriting(configuration.Output,
         configuration.Encoding.GetBytes(".").Length,
         configuration.WriteStreamBufferSize);

      Console.WriteLine("The merge phase runs asynchronously.");
      Stopwatch sw = new Stopwatch();
      sw.Start();
      StreamsMergeExecutorAsync mergerAsync = new StreamsMergeExecutorAsync(configuration, resultWriter);
      Result result = await mergerAsync.MergeWithOrderAsync();
      sw.Stop();
      Console.WriteLine($"Merge completed in {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms");
      
      return result;
   }

   private static Result MergingPhase(IConfig configuration)
   {
      ISeveralTimesLinesWriter resultWriter = LinesWriter.CreateForMultipleWriting(configuration.Output,
         configuration.Encoding.GetBytes(".").Length,
         configuration.WriteStreamBufferSize);

      Console.WriteLine("The merge phase is executed in synchronous mode.");
      Stopwatch sw = new Stopwatch();
      sw.Start();
      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration, resultWriter);
      Result result = merger.MergeWithOrder();
      sw.Stop();
      Console.WriteLine($"Merge completed in {sw.Elapsed.TotalSeconds:F2} sec, {sw.Elapsed.TotalMilliseconds} ms");

      return result;
   }
}