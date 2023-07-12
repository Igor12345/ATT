using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SortingEngine;
using SortingEngine.RuntimeConfiguration;

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
            services.AddHostedService<FileSortingService>();


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
}