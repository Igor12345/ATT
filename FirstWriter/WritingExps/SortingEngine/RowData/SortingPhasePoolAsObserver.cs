using System.Buffers;
using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePoolAsObserver : IAsyncObserver<PreReadPackage>, IAsyncObserver<AfterSortingPhasePackage>,
    IDisposable
{
    private readonly ILogger _logger;
    private readonly SortingPhasePool _pool;

    public SortingPhasePoolAsObserver(SortingPhasePool pool, ILogger logger)
    {
        _pool = Guard.NotNull(pool);
        _logger = Guard.NotNull(logger);
    }
    
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }

    private readonly SimpleAsyncSubject<ReadingPhasePackage> _loadNextChunkSubject =
        new SequentialSimpleAsyncSubject<ReadingPhasePackage>();

    public IAsyncObservable<ReadingPhasePackage> LoadNextChunk => _loadNextChunkSubject;

    public async ValueTask OnNextAsync(PreReadPackage package)
    {
        Console.WriteLine($"-> In SortingPhasePoolManager OnNextAsync PreReadPackage for {package.PackageNumber}, is last: {package.IsLastPackage}");
        
        if(package.IsLastPackage)
            return;
        
        ReadingPhasePackage initialPackage = await _pool.TryAcquireNextAsync();
        
        ReadingPhasePackage nextPackage = initialPackage with { PrePopulatedBytesLength = package.RemainedBytesLength };
        package.RemainedBytes.CopyTo(nextPackage.RowData, 0);

        if (package.RemainedBytes.Length > 0)
            ArrayPool<byte>.Shared.Return(package.RemainedBytes);

        Console.WriteLine($"-> In SortingPhasePoolManager before _loadNextChunkSubject.OnNextAsync for {nextPackage.PackageNumber}, is last: {package.IsLastPackage}");

        await _loadNextChunkSubject.OnNextAsync(nextPackage);
    }

    public async ValueTask OnNextAsync(AfterSortingPhasePackage package)
    {
        Console.WriteLine($"-->! In SortingPhasePoolManager OnNextAsync AfterSortingPhasePackage for {package.PackageNumber}");
        await Log($"Incoming AfterSortingPhasePackage {package.PackageNumber}, is last package: {package.IsLastPackage}");
        
        _pool.ReleaseBuffer(package.ParsedRecords);
        _pool.ReleaseBuffer(package.RowData);
        ArrayPool<LineMemory>.Shared.Return(package.SortedLines);

        if (package.IsLastPackage)
        {
            //todo
            Console.WriteLine($"-> In SortingPhasePoolManager before _loadNextChunkSubject.OnCompletedAsync for {package.PackageNumber}, is last: {package.IsLastPackage}");
            await Task.Delay(20);
            await _loadNextChunkSubject.OnCompletedAsync();
        }
    }

    public ValueTask OnErrorAsync(Exception error)
    {
        return _loadNextChunkSubject.OnCompletedAsync();
    }

    public ValueTask OnCompletedAsync()
    {
        Console.WriteLine($"<---->! In SortingPhasePoolManager OOnCompletedAsync thread: {Thread.CurrentThread.ManagedThreadId}");
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask LetsStart()
    {
        ReadingPhasePackage package = await _pool.TryAcquireNextAsync();
        Console.WriteLine(
            $"-> In SortingPhasePoolManager LetsStart before _loadNextChunkSubject.OnNextAsync for {package.PackageNumber}, is last: {package.IsLastPackage}");
        await _loadNextChunkSubject.OnNextAsync(package);
    }

    public void Dispose()
    {
        _pool.Dispose();
    }
}