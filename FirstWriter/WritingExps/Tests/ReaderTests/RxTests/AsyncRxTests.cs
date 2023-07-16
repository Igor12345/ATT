using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xunit.Abstractions;

namespace ReaderTests;

public class AsyncRxTests
{
    
    private readonly ITestOutputHelper _testOutputHelper;
    public AsyncRxTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task TrySeveralWorkersAsync()
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
        // var t = await AsyncObservable.Range(0, 10)
        //     .Select(i=> LongWork(i, 12)).SubscribeAsync(Extensions.PrintAsync<int>("first"));
        // var t3 = await AsyncObservable.Range(0, 15)
        //     .Select(i => AsyncObservable.FromAsync(async () => await LongWork(i, 22)))
        //     .Merge()
        //     .Do(v => AnotherWork2(v))
        //     .Select(v => AnotherWork(v))
        //     .SubscribeAsync(Extensions.PrintAsync<int>("first"));
        
        
        //!!!!!!!!!!!!!! WORKING !!!!!!!!!!!!!!!
        //parallel execution!!!
        var t2 = await AsyncObservable.Range(0, 10)
            .Select(i => AsyncObservable.FromAsync(async () => await LongWork(i, 22)))
            // .ObserveOn(new SynchronizationContextAsyncScheduler())
            .Merge()
            // .Do(v => AnotherWork2(v))
            .Select(v => AnotherSyncWork(v))
            .Select(v => AnotherWork(v))
            .SubscribeAsync(Extensions.PrintAsync<int>("first", _testOutputHelper, semaphore));

        //single sequence!!!
        var t3 = await AsyncObservable.Range(0, 10)
            // .Select(i => AsyncObservable.FromAsync(async () => await LongWork(i, 22)))
            .Select(async i => await LongWork(i, 22))
            // .Merge()
            // .Do(v => AnotherWork2(v))
            .Select(v => AnotherSyncWork(v))
            .Select(async v => await AnotherWork(v))
            .SubscribeAsync(Extensions.PrintAsync<int>("first", _testOutputHelper, semaphore));


        // var t4 = await AsyncObservable.Range(0, 10)
        //     .SelectMany(i => AsyncObservable.FromAsync<int>(async () => await LongWork(i, 12)))
        //     .SubscribeAsync(Extensions.Print<int>("second"));
        // var t1 = await AsyncObservable.Range(0, 10)
        //     .SubscribeAsync(Extensions.Print<int>("second"));

        await Task.Delay(10);

        await semaphore.WaitAsync();
        Assert.Equal(3, 4);
        // var b = t;
    }
    
    [Fact]
    public async Task TrySeveralWorkers()
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
        
        _testOutputHelper.WriteLine($"Test started ({Thread.CurrentThread.ManagedThreadId})");
        // var t = await AsyncObservable.Range(0, 10)
        //     .Select(i=> LongWork(i, 12)).SubscribeAsync(Extensions.PrintAsync<int>("first"));
        var t3 = Observable.Range(0, 10)
            .Select(i => Observable.FromAsync(async ()=>await LongWork(i, 22)))
            .Merge(4)
            // .Concat()
            // .Do(async v =>await AnotherWork2(v))
            .Select(v => AnotherSyncTask(v))
            
            .Subscribe(PrintSync<int>("first Sync", semaphore));

        // var t4 = await AsyncObservable.Range(0, 10)
        //     .SelectMany(i => AsyncObservable.FromAsync<int>(async () => await LongWork(i, 12)))
        //     .SubscribeAsync(Extensions.Print<int>("second"));
        // var t1 = await AsyncObservable.Range(0, 10)
        //     .SubscribeAsync(Extensions.Print<int>("second"));

        
        _testOutputHelper.WriteLine($"??? Test almost completed ({Thread.CurrentThread.ManagedThreadId})");
        await semaphore.WaitAsync();
        _testOutputHelper.WriteLine($"!!! Test completed ({Thread.CurrentThread.ManagedThreadId})");
        
        
        Assert.Equal(3, 2);
        // await Task.Delay(10);

        // var b = t;
    }
    public IObserver<T> PrintSync<T>(string name, SemaphoreSlim semaphore)
    {
        return Observer.Create<T>(
            x =>
            {
                _testOutputHelper.WriteLine($"OnNext from {name} ({Thread.CurrentThread.ManagedThreadId}): {x}");
                Console.WriteLine($"OnNext from {name} ({Thread.CurrentThread.ManagedThreadId}): {x}");
            },
            ex =>
            {
                _testOutputHelper.WriteLine($"Error by {name} ({Thread.CurrentThread.ManagedThreadId}): {ex}");
                Console.WriteLine($"Error by {name} ({Thread.CurrentThread.ManagedThreadId}): {ex}");
                semaphore.Release();
            },
            () =>
            {
                _testOutputHelper.WriteLine($"Completed in {name} ({Thread.CurrentThread.ManagedThreadId})");
                Console.WriteLine($"Completed in {name} ({Thread.CurrentThread.ManagedThreadId})");
                semaphore.Release();
            }
        );
    }

    private async ValueTask AnotherWork2((int, int) valueTuple)
    {
        await Task.Yield();
        _testOutputHelper.WriteLine($"Inside Do for {valueTuple.Item1}");
        Console.WriteLine($"Inside Do for {valueTuple.Item1}");
    }

    private (T, int) AnotherSyncWork<T>((T, int) valueTuple)
    {
        _testOutputHelper.WriteLine($"Sync Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        Console.WriteLine($"Sync Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        return valueTuple;
    }

    private async ValueTask<T> AnotherWork<T>((T, int) valueTuple)
    {
        await Task.Yield();
        _testOutputHelper.WriteLine($"AnotherWork Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        Console.WriteLine($"AnotherWork Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        return valueTuple.Item1;
    }
    private T AnotherSyncTask<T>((T, int) valueTuple)
    {
        _testOutputHelper.WriteLine($"AnotherWork Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        Console.WriteLine($"AnotherWork Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        return valueTuple.Item1;
    }

    private async Task<T> AnotherWorkTask<T>((T, int) valueTuple)
    {
        await Task.Yield();
        _testOutputHelper.WriteLine($"AnotherWork Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        Console.WriteLine($"AnotherWork Processing {valueTuple.Item1} in ({Thread.CurrentThread.ManagedThreadId}), earlier was: {valueTuple.Item2}");
        return valueTuple.Item1;
    }

    private async ValueTask<(T,int)> LongWork<T>(T value, int maxDelay)
    {
        await Task.Yield();
        _testOutputHelper.WriteLine($"LongWork Processing {value} in ({Thread.CurrentThread.ManagedThreadId})");
        Console.WriteLine($"LongWork Processing {value} in ({Thread.CurrentThread.ManagedThreadId})");
        int delay = Random.Shared.Next(0, maxDelay);
        await Task.Delay(delay);
        _testOutputHelper.WriteLine($"Value {value} was processed in ({Thread.CurrentThread.ManagedThreadId})");
        Console.WriteLine($"Value {value} was processed in ({Thread.CurrentThread.ManagedThreadId})");
        return (value, Thread.CurrentThread.ManagedThreadId);
    }
}