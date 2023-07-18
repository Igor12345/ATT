
using System.Diagnostics;
using FileCreator.Configuration;
using FileCreator.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FileCreator;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args).Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        IBaseConfiguration? baseConfiguration = configuration.GetRequiredSection("Config").Get<IBaseConfiguration>();
        Debug.Assert(baseConfiguration != null, nameof(baseConfiguration) + " != null");
        IRuntimeConfiguration runtimeConfiguration = baseConfiguration.ToRuntime();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(runtimeConfiguration);
                services.AddHostedService<CreatingFileService>();
            })
            .UseSerilog().Build();

        await host.RunAsync();
    }
}