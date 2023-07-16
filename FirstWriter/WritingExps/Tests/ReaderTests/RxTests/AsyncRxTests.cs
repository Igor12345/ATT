using System.ComponentModel.Design;
using System.Reactive.Linq;

namespace ReaderTests;

public class AsyncRxTests
{
    [Fact]
    public async Task TrySeveralWorkers()
    {
        // var t = await AsyncObservable.Range(0, 10)
        //     .Select(i=> LongWork(i, 12)).SubscribeAsync(Extensions.PrintAsync<int>("first"));
        var t3 = await AsyncObservable.Range(0, 15)
            .SelectMany(i => AsyncObservable.FromAsync<int>(async () => await LongWork(i, 22)))
            .SubscribeAsync(Extensions.PrintAsync<int>("first"));

        // var t4 = await AsyncObservable.Range(0, 10)
        //     .SelectMany(i => AsyncObservable.FromAsync<int>(async () => await LongWork(i, 12)))
        //     .SubscribeAsync(Extensions.Print<int>("second"));
        // var t1 = await AsyncObservable.Range(0, 10)
        //     .SubscribeAsync(Extensions.Print<int>("second"));

        await Task.Delay(10);

        // var b = t;
    }

    private async ValueTask<T> LongWork<T>(T value, int delay)
    {
        await Task.Yield();
        Console.WriteLine($"Processing {value} in ({Thread.CurrentThread.ManagedThreadId})");
        await Task.Delay(delay);
        Console.WriteLine($"Value {value} was processed in ({Thread.CurrentThread.ManagedThreadId})");
        return value;
    }
}