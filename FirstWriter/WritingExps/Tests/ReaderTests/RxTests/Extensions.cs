using System.Reactive.Linq;

namespace ReaderTests;

public static class Extensions
{
    /// <summary>
    /// Subscribe an observer that prints each notificatio to the console output
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="name"></param>
    /// <returns>a disposable subscription object</returns>
    public static IDisposable SubscribeConsole<T>(this IObservable<T> observable, string name = "")
    {
        return observable.Subscribe(new ConsoleObserver<T>(name));
    }
    
    public static IAsyncObserver<T> PrintAsync<T>(string name)
    {
        return AsyncObserver.Create<T>(
            async x =>
            {
                await Task.Yield();
                Console.WriteLine($"OnNext from {name} ({Thread.CurrentThread.ManagedThreadId}): {x}");
            },
            async ex =>
            {
                await Task.Yield();
                Console.WriteLine($"Error by {name} ({Thread.CurrentThread.ManagedThreadId}): {ex}");
            },
            async () =>
            {
                await Task.Yield();
                Console.WriteLine($"Completed in {name} ({Thread.CurrentThread.ManagedThreadId})");
            }
        );
    }

    public static IAsyncObserver<T> Print<T>(string name)
    {
        return AsyncObserver.Create<T>(
            x =>
            {
                Console.WriteLine($"OnNext from {name} ({Thread.CurrentThread.ManagedThreadId}): {x}");
                return default;
            },
            ex =>
            {
                Console.WriteLine($"Error by {name} ({Thread.CurrentThread.ManagedThreadId}): {ex}");
                return default;
            },
            () =>
            {
                Console.WriteLine($"Completed in {name} ({Thread.CurrentThread.ManagedThreadId})");
                return default;
            }
        );
    }

    /// <summary>
    /// this method does the same as SubscribeConsole but uses Observable.Subscribe() method instead of a handcrafted observer class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IDisposable SubscribeTheConsole<T>(this IObservable<T> observable, string name = "")
    {

        return observable.Subscribe(
            x => Console.WriteLine("{0} - OnNext({1})", name, x),
            ex =>
            {
                Console.WriteLine("{0} - OnError:", name);
                Console.WriteLine("\t {0}", ex);
            },
            () => Console.WriteLine("{0} - OnCompleted()", name));
    }

    /// <summary>
    /// Adds a log that prints to the console the notification emitted by the <paramref name="observable"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="msg">An optioanl prefix that will be added before each notification</param>
    /// <returns></returns>
    public static IObservable<T> Log<T>(this IObservable<T> observable, string msg = "")
    {
        return observable.Do(
            x => Console.WriteLine("{0} - OnNext({1})", msg, x),
            ex =>
            {
                Console.WriteLine("{0} - OnError:", msg);
                Console.WriteLine((string)"\t {0}", (object?)ex);
            },
            () => Console.WriteLine("{0} - OnCompleted()", msg));
    }

    /// <summary>
    /// Logs the subscriptions and emissions done on/by the observable
    /// each log message also includes the thread it happens on
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static IObservable<T> LogWithThread<T>(this IObservable<T> observable, string msg = "")
    {
        return Observable.Defer(() =>
        {
            Console.WriteLine("{0} Subscription happened on Thread: {1}", msg, 
                Thread.CurrentThread.ManagedThreadId);

            return observable.Do(
                x => Console.WriteLine("{0} - OnNext({1}) Thread: {2}", msg, x,
                    Thread.CurrentThread.ManagedThreadId),
                ex =>
                {
                    Console.WriteLine("{0} - OnError Thread:{1}", msg,
                        Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("\t {0}", ex);
                },
                () => Console.WriteLine("{0} - OnCompleted() Thread {1}", msg,
                    Thread.CurrentThread.ManagedThreadId));
        });
    }

    /// <summary>
    /// Runs a configureable action when the observable completes or emit error 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observable">the source observable</param>
    /// <param name="lastAction">an action to perform when the source observable completes or has error</param>
    /// <param name="delay">the time span to wait before invoking the <paramref name="lastAction"/></param>
    /// <returns></returns>
    public static IObservable<T> DoLast<T>(this IObservable<T> observable, Action lastAction, TimeSpan? delay = null)
    {
        Action delayedLastAction = async () =>
        {
            if (delay.HasValue)
            {
                await Task.Delay(delay.Value);
            }
            lastAction();
        };
        return observable.Do(
            (_) => { },//empty OnNext
            _ => delayedLastAction(),
            delayedLastAction);
    }



    public static void RunExample<T>(this IObservable<T> observable, string exampleName = "")
    {
        var exampleResetEvent = new AutoResetEvent(false);

        observable
            .DoLast(() => exampleResetEvent.Set(), TimeSpan.FromSeconds(3))
            .SubscribeConsole(exampleName);

        exampleResetEvent.WaitOne();

    }
}