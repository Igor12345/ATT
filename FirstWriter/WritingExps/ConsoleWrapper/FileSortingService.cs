using System.Diagnostics;
using System.Reactive.Linq;
using ConsoleWrapper.IOProcessing;
using Infrastructure.MemoryTools;
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
      InputParametersValidator inputParametersValidator = new InputParametersValidator();
      (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

      if (!canContinue)
         return;
         
      Console.WriteLine("--> Ready to start");
      var r = Console.ReadLine();

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer();
      IConfig configuration = analyzer.SuggestConfig(validInput);

      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration);
      
      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration.TemporaryFolder, cancellationToken);
      await using ResultWriter resultWriter = ResultWriter.Create(configuration.Output, cancellationToken);
      
      merger.OutputBufferFull += (o, eventArgs) =>
      {
         Console.WriteLine("Writing merge result");
         resultWriter.WriteOutput(eventArgs);
      };
      
      Stopwatch sw = Stopwatch.StartNew();

      await using (IBytesProducer bytesReader = new LongFileReader(validInput.File, validInput.Encoding))
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

      IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer();
      IConfig configuration = analyzer.SuggestConfig(validInput);

      RecordsExtractorAsSequence extractor = new RecordsExtractorAsSequence(
         configuration.Encoding.GetBytes(Environment.NewLine),
         configuration.Encoding.GetBytes(". "), cancellationToken);
      
      
      
      StreamsMergeExecutor merger = new StreamsMergeExecutor(configuration);
      
      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration.TemporaryFolder, cancellationToken);
      await using ResultWriter resultWriter = ResultWriter.Create(configuration.Output, cancellationToken);

      await using IBytesProducer bytesReader = new LongFileReader(validInput.File, validInput.Encoding, cancellationToken);
      LinesSorter sorter = new LinesSorter();

      InputBuffersManager buffersManager = new InputBuffersManager(3, configuration.InputBufferLength,
         configuration.RecordsBufferLength, cancellationToken);
      
      var s6 = await buffersManager.LoadNextChunk.SubscribeAsync(bytesReader);
      var s3 = await bytesReader.NextChunkPrepared.SubscribeAsync(extractor);
      
      var s1 = await extractor.ReadyForSorting.SubscribeAsync(sorter);
      var s2 = await extractor.ReadyForNextChunk.SubscribeAsync(buffersManager);
      var s4 = await sorter.SortingCompleted.SubscribeAsync(resultWriter);
      var s5 = await resultWriter.SortedLinesSaved.SubscribeAsync(buffersManager);

      await bytesReader.NextChunkPrepared.SubscribeAsync(
         b => { },
         () =>
         {
            MemoryCleaner.CleanMemory();
            StartMerge();
         }
      );

      await buffersManager.LetsStart();

   }

   private void StartMerge()
   {
      Console.WriteLine("---> Ready for merge! <---");
   }
}