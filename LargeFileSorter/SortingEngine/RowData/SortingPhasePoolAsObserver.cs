using System.Buffers;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Channels;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePoolAsObserver : IDisposable
{
    private readonly SortingPhasePool _pool;
    private readonly ReadyProcessingNextChunkObserver _readyProcessingNextChunkObserver;
    private readonly ReadyReleaseBuffersObserver _releaseBuffersObserver;
    private readonly Channel<ReadingPhasePackage> _packagesQueue = Channel.CreateUnbounded<ReadingPhasePackage>();
    
    public SortingPhasePoolAsObserver(SortingPhasePool pool)
    {
        _pool = Guard.NotNull(pool);
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
            if (package.IsLastPackage)
            {
                ReadingPhasePackage last = new ReadingPhasePackage(Array.Empty<byte>(),
                    ExpandingStorage<Line>.Empty, package.PackageNumber, true);
                await _writer.WriteAsync(last);
                
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

            await _writer.WriteAsync(nextPackage);
        }

        public ValueTask OnErrorAsync(Exception ex) => throw ex;

        public ValueTask OnCompletedAsync() => ValueTask.CompletedTask;
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

        public ValueTask OnNextAsync(AfterSortingPhasePackage package)
        {
            ReleaseTakenStorages(package);

            if (package.IsLastPackage)
                _writer.Complete();

            return ValueTask.CompletedTask;
        }

        private void ReleaseTakenStorages(AfterSortingPhasePackage package)
        {
            _poolAsObserver._pool.ReleaseBuffer(package.ParsedRecords);
            _poolAsObserver._pool.ReuseBuffer(package.RowData);
            ArrayPool<Line>.Shared.Return(package.SortedLines);
        }

        //Here can be some smart handler
        public ValueTask OnErrorAsync(Exception ex) => throw ex;

        public ValueTask OnCompletedAsync() => ValueTask.CompletedTask;
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
        ChannelReader<ReadingPhasePackage> reader = _packagesQueue.Reader;

        await Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                object checkedState = Guard.NotNull(state);

                var (observer, reader, token) =
                    (Tuple<IAsyncObserver<ReadingPhasePackage>, ChannelReader<ReadingPhasePackage>, CancellationToken>)
                    checkedState;

                while (await reader.WaitToReadAsync(token))
                {
                    ReadingPhasePackage package = await reader.ReadAsync(token);
                    if (package.IsLastPackage)
                    {
                        await observer.OnCompletedAsync();
                        break;
                    }

                    await observer.OnNextAsync(package);
                }

                return true;
            },
            new Tuple<IAsyncObserver<ReadingPhasePackage>, ChannelReader<ReadingPhasePackage>, CancellationToken>(
                asyncObserver, reader, token), token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);

        return AsyncDisposable.Nop;
    }

    public async ValueTask LetsStartAsync(CancellationToken cancellationToken)
    {
        _pool.Run(cancellationToken);
        ReadingPhasePackage package = await _pool.TryAcquireNextAsync();
        await _packagesQueue.Writer.WriteAsync(package);
    }

    public void Dispose() => _pool.Dispose();
}