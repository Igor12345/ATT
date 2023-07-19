using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SortingEngine.RuntimeConfiguration;

namespace ConsoleWrapper
{
   internal class Program
   {
      static async Task Main(string[] args)
      {
         IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

         hostBuilder.ConfigureAppConfiguration((_, configBuilder) =>
         {
            configBuilder.AddEnvironmentVariables();
            configBuilder.AddCommandLine(args);
         });
         hostBuilder.ConfigureLogging((_, configLogging) =>
         {
            configLogging.AddConsole();
            configLogging.AddDebug();
         });

         hostBuilder.ConfigureServices((context, services) =>
         {
            services.Configure<BaseConfiguration>(context.Configuration.GetSection("TechSettings"));
            services.Configure<InputParameters>(context.Configuration.GetSection("Input"));
            services.AddHostedService<FileSortingService>();
         });

         IHost host = hostBuilder.Build();
         await host.RunAsync();
      }
   }
}