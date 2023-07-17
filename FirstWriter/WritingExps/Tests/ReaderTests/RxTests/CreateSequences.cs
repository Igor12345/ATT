using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ReaderTests;

public class CreateSequences
{
    public static IAsyncObservable<int> CreateEndless(CancellationToken token)
    {
        var seq = AsyncObservable.Create<int>(observer => EndlessLoop(observer, token));

        return seq;
    }

    private static async ValueTask<IAsyncDisposable> EndlessLoop(IAsyncObserver<int> observer, CancellationToken token)
    {
        int i = 0;
        while (true)
        {
            await observer.OnNextAsync(i);
            if(token.IsCancellationRequested)
                break;
            i++;
        }

        return AsyncDisposable.Nop;
    }
}