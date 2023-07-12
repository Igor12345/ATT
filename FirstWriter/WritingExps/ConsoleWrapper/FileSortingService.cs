using System.Diagnostics;
using ConsoleWrapper.IOProcessing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SortingEngine;
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

      RecordsSetSorter sorter = new RecordsSetSorter(configuration);

      IntermediateResultsDirector chunksDirector =
         IntermediateResultsDirector.Create(configuration.TemporaryFolder, cancellationToken);
      await using ResultWriter resultWriter = ResultWriter.Create(validInput.File, cancellationToken);
      sorter.SortingCompleted += (o, eventArgs) =>
      {
         Console.WriteLine("Writing chunk");
         chunksDirector.WriteRecords(eventArgs);
      };
      sorter.OutputBufferFull += (o, eventArgs) =>
      {
         Console.WriteLine("Writing result");
         resultWriter.WriteOutput(eventArgs);
      };
      IBytesProducer bytesReader = new LongFileReader(validInput.File, validInput.Encoding);

      Console.WriteLine("Before starting");
      Stopwatch sw = Stopwatch.StartNew();

      var result = await sorter.SortAsync(bytesReader, cancellationToken);

      sw.Stop();
      Console.WriteLine(result.Success
         ? $"---> Success - {sw.Elapsed.TotalMinutes} min, {sw.Elapsed.Seconds} sec; Total: {sw.Elapsed.TotalSeconds} sec, {sw.Elapsed.TotalMilliseconds} ms"
         : $"---> Error: {result.Message}");



      await Task.Delay(2);
   }

      

   public async Task StopAsync(CancellationToken cancellationToken)
   {
      //todo
      Console.WriteLine("Bye service");
      await Task.Delay(2);
   }
}