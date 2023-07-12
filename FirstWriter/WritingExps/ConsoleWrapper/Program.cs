using System.Diagnostics;
using System.IO;
using System.Text;
using ConsoleWrapper.IOProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SortingEngine;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.RuntimeEnvironment;

namespace ConsoleWrapper
{
   internal class Program
   {
      static async Task Main(string[] args)
      {
         //todo!!! handle lack eol on the last line
         //do not split small files

         IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

         // hostBuilder.ConfigureHostConfiguration((configBuilder) =>
         //    configBuilder.AddEnvironmentVariables());

         hostBuilder.ConfigureAppConfiguration((context, configBuilder) =>
         {
            
            configBuilder.AddCommandLine(args);
            
            var s = configBuilder.Sources;
         });
         hostBuilder.ConfigureLogging((hostContext, configLogging) =>
         {
            configLogging.AddConsole();
            configLogging.AddDebug();
         });

         

         hostBuilder.ConfigureServices((context, services) =>
         {
            
            // services.AddSingleton<BaseConfiguration>();
            var section = context.Configuration.GetSection("EnvironmentSettings");
            services.Configure<BaseConfiguration>(context.Configuration.GetSection("EnvironmentSettings"));
            services.Configure<InputParameters>(context.Configuration.GetSection("Input"));
            var conf = context.Configuration;
            // services.AddOptions<BaseConfiguration>("base")
            //    .Bind(context.Configuration.GetSection("EnvironmentSettings"));
            // services.AddOptions<InputParameters>("input")
            //    .Bind(context.Configuration.GetSection("Input"));
            services.AddHostedService<Worker>();


            var conf1 = context.Configuration;
         });
         // hostBuilder.ConfigureAppConfiguration()

         HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
         

         // builder.Services.AddHostedService<Worker>();
         // var config = builder.Configuration;
         IHost host = hostBuilder.Build();

         var scope = host.Services.CreateScope();
         var config = scope.ServiceProvider.GetService<BaseConfiguration>();

         host.Run();

         return;

         

         
      }

      private static void SortingCompleted(object? sender, SortingCompletedEventArgs e)
      {
         throw new NotImplementedException();
      }
   }

   internal class Worker : IHostedService
   {
      private readonly BaseConfiguration _baseOptions;
      private readonly InputParameters _input;

      public Worker(IOptions<BaseConfiguration> baseOptions, IOptions<InputParameters> input)
      {
         _baseOptions = baseOptions.Value;
         _input = input.Value;
      }
      public async Task StartAsync(CancellationToken cancellationToken)
      {
         Console.WriteLine("Hi from service");
         Console.WriteLine($"Encoding: {_input.Encoding}");
         Console.WriteLine($"File: {_input.FileName}");
         Console.ReadLine();

         var inputParametersValidator = new InputParametersValidator();
         (bool canContinue, ValidatedInputParameters validInput) = inputParametersValidator.CheckInputParameters(_input);

         if (!canContinue)
            return;

         CancellationTokenSource cts = new CancellationTokenSource();

         IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer();
         var configuration = analyzer.SuggestConfig(validInput);

         RecordsSetSorter sorter = new RecordsSetSorter(configuration);
         IntermediateResultsDirector chunksDirector =
            IntermediateResultsDirector.Create(configuration.TemporaryFolder, cts.Token);
         await using ResultWriter resultWriter = ResultWriter.Create(validInput.File, cts.Token);
         sorter.SortingCompleted += (o, eventArgs) => chunksDirector.WriteRecords(eventArgs);
         sorter.OutputBufferFull += (o, eventArgs) => resultWriter.WriteOutput(eventArgs);
         IBytesProducer bytesReader = new LongFileReader(validInput.File, validInput.Encoding);

         Stopwatch sw = Stopwatch.StartNew();

         var result = await sorter.SortAsync(bytesReader, cts.Token);

         sw.Stop();
         Console.WriteLine(result.Success
            ? $"---> Success - {sw.Elapsed.TotalMinutes} min, {sw.Elapsed.Seconds} sec; Total: {sw.Elapsed.TotalSeconds} sec, {sw.Elapsed.TotalMilliseconds} ms"
            : $"---> Error: {result.Message}");



         await Task.Delay(2);
      }

      

      public async Task StopAsync(CancellationToken cancellationToken)
      {
         Console.WriteLine("Bye service");
         await Task.Delay(2);
      }
   }

   public class InputParametersValidator
   {

      public (bool, ValidatedInputParameters) CheckInputParameters(InputParameters input)
      {
         string path = input.FileName ?? "";

         while (true)
         {
            if (File.Exists(path))
            {
               break;
            }

            Console.WriteLine("Hi, enter the full name of the file. Or 'X' to exit.");
            //todo
            path = Console.ReadLine() ?? "";
            if (path.ToUpper() == "X")
               return (false, ValidatedInputParameters.Empty);
            if (!string.IsNullOrEmpty(path))
            {
               if (File.Exists(path))
               {
                  break;
               }

               Console.WriteLine("File does not exist");
            }
         }

         Encoding encoding;
         string encodingName = input.Encoding;
         while (true)
         {
            if (TrySelectEncoding(encodingName, out encoding))
            {
               break;
            }

            Console.WriteLine("Enter encoding or Y if ASCII or X to exit");
            encodingName = Console.ReadLine() ?? "";
            if (encodingName.ToUpper() == "X")
               return (false, ValidatedInputParameters.Empty);
            if (TrySelectEncoding(encodingName, out encoding))
            {
               break;
            }

            Console.WriteLine("Encoding does not exist");
         }
         return (true, new ValidatedInputParameters(path, encoding));
      }

      //todo another class
      private static bool TrySelectEncoding(string encodingName, out Encoding encoding)
      {
         if (string.Equals(encodingName, "ASCII", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(encodingName, "UTF8", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(encodingName, "Y", StringComparison.OrdinalIgnoreCase))
         {
            encoding = Encoding.UTF8;
            return true;
         }

         if (string.Equals(encodingName, "UTF32", StringComparison.OrdinalIgnoreCase))
         {
            encoding = Encoding.UTF32;
            return true;
         }

         encoding = Encoding.Default;
         return false;
      }
   }
}