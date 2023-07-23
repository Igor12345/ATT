using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace SimpleBenchmarks.ByteOperations;

public class InProcessNoEmitConfig : ManualConfig
{
    public InProcessNoEmitConfig()
    {
        AddJob(Job.MediumRun
            .WithToolchain(InProcessEmitToolchain.Instance));
    }
}