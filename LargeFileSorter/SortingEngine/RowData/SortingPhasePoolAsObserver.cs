using System.Buffers;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Channels;
using Infrastructure.Parameters;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class SortingPhasePoolAsObserver : IDisposable
{
    private readonly SortingPhasePool _pool;
    private readonly int _maxLineLength;
    private readonly ReadyProcessingNextChunkObserver _readyProcessingNextChunkObserver;
    private readonly ReadyReleaseBuffersObserver _releaseBuffersObserver;
    private readonly Channel<ReadyForExtractionPackage> _packagesQueue = Channel.CreateUnbounded<ReadyForExtractionPackage>();
    
    public SortingPhasePoolAsObserver(SortingPhasePool pool, int maxLineLength)
    {
        _pool = Guard.NotNull(pool);
        _maxLineLength = Guard.Positive(maxLineLength);
        _readyProcessingNextChunkObserver = new ReadyProcessingNextChunkObserver(this);
        _releaseBuffersObserver = new ReadyReleaseBuffersObserver(this);
    }

    private class ReadyProcessingNextChunkObserver : IAsyncObserver<PreReadPackage>
    {
        private readonly SortingPhasePoolAsObserver _poolAsObserver;
        private readonly ChannelWriter<ReadyForExtractionPackage> _writer;

        public ReadyProcessingNextChunkObserver(SortingPhasePoolAsObserver poolAsObserver)
        {
            _poolAsObserver = Guard.NotNull(poolAsObserver);
            _writer = _poolAsObserver._packagesQueue.Writer;
        }

        public async ValueTask OnNextAsync(PreReadPackage package)
        {
            if (package.IsLastPackage)
            {
                ReadyForExtractionPackage last = ReadyForExtractionPackage.Empty with { IsLastPackage = true };
                await _writer.WriteAsync(last);
                
                return;
            }

            FilledBufferPackage initialPackage = await _poolAsObserver._pool.TryAcquireNextFilledBufferAsync(package.Id);

            //todo replace
            ReadyForExtractionPackage nextPackage = new ReadyForExtractionPackage(initialPackage,
                (_poolAsObserver._maxLineLength - package.RemainedBytesLength)..(_poolAsObserver._maxLineLength +
                    initialPackage.WrittenBytesLength));
            
            package.RemainedBytes.AsMemory(..package.RemainedBytesLength).CopyTo(nextPackage.LineData);

            if (package.RemainedBytes.Length > 0)
                ArrayPool<byte>.Shared.Return(package.RemainedBytes);

            await _writer.WriteAsync(nextPackage);
        }

        public ValueTask OnErrorAsync(Exception ex) => throw ex;

        public ValueTask OnCompletedAsync() => ValueTask.CompletedTask;
    }

    private class ReadyReleaseBuffersObserver : IAsyncObserver<AfterSavingBufferPackage>
    {
        private readonly SortingPhasePoolAsObserver _poolAsObserver;
        private readonly ChannelWriter<ReadyForExtractionPackage> _writer;

        public ReadyReleaseBuffersObserver(SortingPhasePoolAsObserver poolAsObserver)
        {
            _poolAsObserver = Guard.NotNull(poolAsObserver);
            _writer = _poolAsObserver._packagesQueue.Writer;
        }

        public ValueTask OnNextAsync(AfterSavingBufferPackage package)
        {
            ReleaseTakenStorages(package);

            if (package.IsLastPackage)
                _writer.Complete();

            return ValueTask.CompletedTask;
        }

        private void ReleaseTakenStorages(AfterSavingBufferPackage package)
        {
            _poolAsObserver._pool.ReleaseBuffer(package.ParsedRecords);
            _poolAsObserver._pool.ReuseBuffer(package.RowData);
            ArrayPool<Line>.Shared.Return(package.BufferForSortedLines);
        }

        //Here can be some smart handler
        public ValueTask OnErrorAsync(Exception ex) => throw ex;

        public ValueTask OnCompletedAsync() => ValueTask.CompletedTask;
    }

    public IAsyncObserver<PreReadPackage> ReadyProcessingNextChunk => _readyProcessingNextChunkObserver;

    public IAsyncObserver<AfterSavingBufferPackage> ReleaseBuffers => _releaseBuffersObserver;

    public IAsyncObservable<ReadyForExtractionPackage> StreamLinesByBatches(CancellationToken token)
    {
        return AsyncObservable.Create<ReadyForExtractionPackage>(observer => EndlessReading(observer, token));
    }

    private async ValueTask<IAsyncDisposable> EndlessReading(IAsyncObserver<ReadyForExtractionPackage> asyncObserver,
        CancellationToken token)
    {
        ChannelReader<ReadyForExtractionPackage> reader = _packagesQueue.Reader;
        //todo
        Console.WriteLine(
            $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) SortingPhasePoolAsObserver entering EndlessReading");


        await Task.Factory.StartNew<Task<bool>>(static async (state) =>
            {
                object checkedState = Guard.NotNull(state);

                var (observer, reader, token) =
                    (Tuple<IAsyncObserver<ReadyForExtractionPackage>, ChannelReader<ReadyForExtractionPackage>, CancellationToken>)
                    checkedState;

                //todo
                Console.WriteLine(
                    $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) SortingPhasePoolAsObserver before endless loop");
                while (await reader.WaitToReadAsync(token))
                {
                    //todo
                    Console.WriteLine(
                        $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) SortingPhasePoolAsObserver reader reading a package");
                    ReadyForExtractionPackage package = await reader.ReadAsync(token);
                    
                    //todo
                    Console.WriteLine(
                        $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) SortingPhasePoolAsObserver " +
                        $"reader read package {package.Id} with {package.WrittenBytesLength} bytes, is last: {package.IsLastPackage}, sending as ReadyForExtractionPackage");
                    
                    if (package.IsLastPackage)
                    {
                        await observer.OnCompletedAsync();
                        break;
                    }
                    
                    await observer.OnNextAsync(package);
                }

                //todo
                Console.WriteLine(
                    $"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) SortingPhasePoolAsObserver AFTER endless loop !!!!!");
                return true;
            },
            new Tuple<IAsyncObserver<ReadyForExtractionPackage>, ChannelReader<ReadyForExtractionPackage>, CancellationToken>(
                asyncObserver, reader, token), token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);

        return AsyncDisposable.Nop;
    }

    public async ValueTask LetsStartAsync(CancellationToken cancellationToken)
    {
        _pool.Run(cancellationToken);
        FilledBufferPackage package = await _pool.TryAcquireNextFilledBufferAsync(-1);
        ReadyForExtractionPackage first =
            new ReadyForExtractionPackage(package, _maxLineLength..(_maxLineLength + package.WrittenBytesLength));
        await _packagesQueue.Writer.WriteAsync(first, cancellationToken);
    }

    public void Dispose() => _pool.Dispose();
}