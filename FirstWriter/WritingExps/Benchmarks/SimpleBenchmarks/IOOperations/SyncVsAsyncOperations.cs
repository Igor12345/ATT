using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace SimpleBenchmarks.IOOperations;

// [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2)]
// [Config(typeof(InProcessNoEmitConfig))]
public class SyncVsAsyncOperations
{
    private const string FilePathSync1 = "testSync1.data";
    private const string FilePathSync2 = "testSync2.data";
    private const string FilePathSync3 = "testSync3.data";
    private const string FilePathAsync1 = "testAsync1.data";
    private const string FilePathAsync2 = "testAsync2.data";
    private const string FilePathAsync3 = "testAsync3.data";
    private byte[]? _userBuffer;
    
    [Params(4096, 16_000, 64_000, 256_000, 1_048_576)]
    public int UserBuffer;
    
    [GlobalSetup]
    public void Setup()
    {
        _userBuffer = new byte[UserBuffer];
        File.WriteAllBytes(FilePathSync1, new byte[100_000_000]); 
        File.WriteAllBytes(FilePathSync2, new byte[100_000_000]); 
        File.WriteAllBytes(FilePathSync3, new byte[100_000_000]); 
        File.WriteAllBytes(FilePathAsync1, new byte[100_000_000]); 
        File.WriteAllBytes(FilePathAsync2, new byte[100_000_000]); 
        File.WriteAllBytes(FilePathAsync3, new byte[100_000_000]); 
    }
    
    [GcServer(true)]
    [Benchmark]
    public async Task ReadAsyncModeDefaultBuffer()
    {
        await using FileStream fs = new FileStream(FilePathSync1, FileMode.Open,
            FileAccess.Read, FileShare.None, bufferSize: 4096, true);
        while (await fs.ReadAsync(_userBuffer) != 0) ;
    }
    
    [GcServer(true)]
    [Benchmark]
    public async Task ReadAsyncModeNoBuffer()
    {
        await using FileStream fs = new FileStream(FilePathSync2, FileMode.Open,
            FileAccess.Read, FileShare.None, bufferSize: 1, true);
        while (await fs.ReadAsync(_userBuffer) != 0) ;
    }
    
    [GcServer(true)]
    [Benchmark]
    public async Task ReadAsyncModeBiggerBuffer()
    {
        await using FileStream fs = new FileStream(FilePathSync3, FileMode.Open,
            FileAccess.Read, FileShare.None, bufferSize: 16_000, true);
        while (await fs.ReadAsync(_userBuffer) != 0) ;
    }
    
    [GcServer(true)]
    [Benchmark]
    public void ReadSyncModeDefaultBuffer()
    {
        using FileStream fs = new FileStream(FilePathSync1, FileMode.Open,
            FileAccess.Read, FileShare.None, bufferSize: 4096, false);
        while (fs.Read(_userBuffer) != 0) ;
    }
    
    [GcServer(true)]
    [Benchmark]
    public void ReadSyncModeNoBuffer()
    {
        using FileStream fs = new FileStream(FilePathSync2, FileMode.Open,
            FileAccess.Read, FileShare.None, bufferSize: 1, false);
        while (fs.Read(_userBuffer) != 0) ;
    }
    
    [GcServer(true)]
    [Benchmark(Baseline = true)]
    public void ReadSyncModeBiggerBuffer()
    {
        using FileStream fs = new FileStream(FilePathSync3, FileMode.Open,
            FileAccess.Read, FileShare.None, bufferSize: 16_000, false);
        while (fs.Read(_userBuffer) != 0) ;
    }

}