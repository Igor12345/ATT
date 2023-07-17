using System.Buffers;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePoolAsObserver : IDisposable
{
    private readonly ILogger _logger;
    private readonly SortingPhasePool _pool;
    private readonly ReadyProcessingNextChunkObserver _readyProcessingNextChunkObserver;
    private readonly ReadyReleaseBuffersObserver _releaseBuffersObserver;
    private readonly Channel<ReadingPhasePackage> _packagesQueue = Channel.CreateUnbounded<ReadingPhasePackage>();
    
    public SortingPhasePoolAsObserver(SortingPhasePool pool, ILogger logger)
    {
        _pool = Guard.NotNull(pool);
        _logger = Guard.NotNull(logger);
        _readyProcessingNextChunkObserver = new ReadyProcessingNextChunkObserver(this);
        _releaseBuffersObserver = new ReadyReleaseBuffersObserver(this);
    }

    private class ReadyProcessingNextChunkObserver : IAsyncObserver<PreReadPackage>
    {
        private readonly SortingPhasePoolAsObserver _poolAsObserver;
        private readonly ChannelWriter<ReadingPhasePackage> _writer;

        public ReadyProcessingNextChunkObserver(SortingPhasePoolAsObserver poolAsObserver)
        {
            _poolAsObserver = Guard.NotNull(poolAsObserver);
            _writer = _poolAsObserver._packagesQueue.Writer;
        }

        public async ValueTask OnNextAsync(PreReadPackage package)
        {
            Console.WriteLine(
                $"--> In SortingPhasePoolAsObserver OnNextAsync PreReadPackage for {package.PackageNumber}, " +
                $"is last: {package.IsLastPackage}, contains bytes: {package.RemainedBytesLength}");

            if (package.IsLastPackage)
            {
                ReadingPhasePackage last = new ReadingPhasePackage(Array.Empty<byte>(),
                    ExpandingStorage<LineMemory>.Empty, package.PackageNumber, true);
                await _writer.WriteAsync(last);
                
                //todo complete? can we lost unprocessed jobs?
                return;
            }

            ReadingPhasePackage initialPackage = await _poolAsObserver._pool.TryAcquireNextAsync();

            ReadingPhasePackage nextPackage = initialPackage with
            {
                PrePopulatedBytesLength = package.RemainedBytesLength, IsLastPackage = package.IsLastPackage
            };
            package.RemainedBytes.CopyTo(nextPackage.RowData, 0);

            if (package.RemainedBytes.Length > 0)
                ArrayPool<byte>.Shared.Return(package.RemainedBytes);

            Console.WriteLine(
                $"--> In SortingPhasePoolManager before _loadNextChunkSubject.OnNextAsync for {nextPackage.PackageNumber}, is last: {nextPackage.IsLastPackage}");

            await _writer.WriteAsync(nextPackage);
        }

        public ValueTask OnErrorAsync(Exception error)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"!!!!!!!!! Error {error}");
            Console.ForegroundColor = color;
            //todo write special error package
            // return _poolAsObserver._loadNextChunkSubject.OnCompletedAsync();
            _writer.Complete();
            return default;
        }

        public ValueTask OnCompletedAsync()
        {
            Console.WriteLine(
                $"<---->! In SortingPhasePoolManager OOnCompletedAsync thread: {Thread.CurrentThread.ManagedThreadId}");

            return ValueTask.CompletedTask;
        }
    }

    private class ReadyReleaseBuffersObserver : IAsyncObserver<AfterSortingPhasePackage>
    {
        private readonly SortingPhasePoolAsObserver _poolAsObserver;
        private readonly ChannelWriter<ReadingPhasePackage> _writer;

        public ReadyReleaseBuffersObserver(SortingPhasePoolAsObserver poolAsObserver)
        {
            _poolAsObserver = Guard.NotNull(poolAsObserver);
            _writer = _poolAsObserver._packagesQueue.Writer;
        }

        public async ValueTask OnNextAsync(AfterSortingPhasePackage package)
        {
            int bufferId = package.RowData.GetHashCode();
            int num = package.PackageNumber;
            Console.WriteLine(
                $"-->! In SortingPhasePoolManager OnNextAsync AfterSortingPhasePackage for {package.PackageNumber}, is last: {package.IsLastPackage}, bufferId: {bufferId}");
            await _poolAsObserver.Log(
                $"Incoming AfterSortingPhasePackage {package.PackageNumber}, is last package: {package.IsLastPackage}");

            ReleaseTakenStorages(package);

            if (package.IsLastPackage)
            {
                //todo
                Console.WriteLine(
                    $"--> !!! In SortingPhasePoolManager before _loadNextChunkSubject.OnCompletedAsync for {package.PackageNumber}, is last: {package.IsLastPackage}");
                
                _writer.Complete();
            }
        }

        private void ReleaseTakenStorages(AfterSortingPhasePackage package)
        {
            _poolAsObserver._pool.ReleaseBuffer(package.ParsedRecords);
            _poolAsObserver._pool.ReleaseBuffer(package.RowData);
            ArrayPool<LineMemory>.Shared.Return(package.SortedLines);
        }

        public ValueTask OnErrorAsync(Exception error)
        {
            //todo
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"!!!!!!!!! Error {error}");
            Console.ForegroundColor = color;
            _writer.Complete();
            return default;
        }

        public ValueTask OnCompletedAsync()
        {
            Console.WriteLine(
                $"<---->! In SortingPhasePoolManager OOnCompletedAsync thread: {Thread.CurrentThread.ManagedThreadId}");

            return ValueTask.CompletedTask;
        }
    }

    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }

    public IAsyncObserver<PreReadPackage> ReadyProcessingNextChunk => _readyProcessingNextChunkObserver;

    public IAsyncObserver<AfterSortingPhasePackage> ReleaseBuffers => _releaseBuffersObserver;

    public IAsyncObservable<ReadingPhasePackage> StreamLinesByBatches(CancellationToken token)
    {
        return AsyncObservable.Create<ReadingPhasePackage>(observer => EndlessReading(observer, token));
    }

    private async ValueTask<IAsyncDisposable> EndlessReading(IAsyncObserver<ReadingPhasePackage> asyncObserver,
        CancellationToken token)
    {
        Console.WriteLine($"Enter EndlessReading");
        ChannelReader<ReadingPhasePackage> reader = _packagesQueue.Reader;

        await Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                object checkedState = Guard.NotNull(state);

                var (observer, reader, token) =
                    (Tuple<IAsyncObserver<ReadingPhasePackage>, ChannelReader<ReadingPhasePackage>, CancellationToken>)
                    checkedState;

                Console.WriteLine($"Enter EndlessReading before loop");
                while (await reader.WaitToReadAsync(token))
                {
                    ReadingPhasePackage package = await reader.ReadAsync(token);
                    int id = package.RowData.GetHashCode();
                    
                    Console.WriteLine($"EndlessReading next package: {package.PackageNumber}, " +
                                      $"last: {package.IsLastPackage}, buffer Id: {id}");
                    if (package.IsLastPackage)
                    {
                        await observer.OnCompletedAsync();
                        break;
                    }

                    await observer.OnNextAsync(package);
                }

                Console.WriteLine($" EndlessReading after loop ------------------>");
                return true;
            },
            new Tuple<IAsyncObserver<ReadingPhasePackage>, ChannelReader<ReadingPhasePackage>, CancellationToken>(
                asyncObserver, reader, token), token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);

        return AsyncDisposable.Nop;
    }

    public async ValueTask LetsStart()
    {
        ReadingPhasePackage package = await _pool.TryAcquireNextAsync();
        Console.WriteLine(
            $"-> In SortingPhasePoolManager LetsStart before _loadNextChunkSubject.OnNextAsync for {package.PackageNumber}, is last: {package.IsLastPackage}");
        await _packagesQueue.Writer.WriteAsync(package);
    }

    public void Dispose()
    {
        _pool.Dispose();
    }
}