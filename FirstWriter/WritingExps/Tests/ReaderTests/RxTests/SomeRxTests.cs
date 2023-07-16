using System.Reactive;
using System.Reactive.Subjects;

namespace ReaderTests;

public class SomeRxTests
{
    [Fact]
    public void CheckComplete()
    {
        var numbers = new NumbersObservable(5);
        var subscription =
            numbers.Subscribe(new ConsoleObserver<int>("numbers"));
         
        var subscription2 =
            numbers.Subscribe(new ConsoleObserver<int>("numbers 2"));
        var t = 3;
    }

    [Fact]
    public void SubjectsTests()
    {
        Subject<int> sbj = new Subject<int>();

        sbj.SubscribeConsole("First");
        sbj.SubscribeConsole("Second");

        sbj.OnNext(1);
        sbj.OnNext(2);
        sbj.OnCompleted();

        var t = 3;
    }
}