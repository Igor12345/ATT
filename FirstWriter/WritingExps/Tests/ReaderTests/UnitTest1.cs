using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using SimpleReader;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.Sorters;

namespace ReaderTests
{
   public class UnitTest1
   {
      [Fact]
      public async Task CanRead()
      {
         InputProcessor inputProcessor = new InputProcessor();
         string fileName = "fourth";
         string path = @$"d://temp/ATT/{fileName}.txt";
         byte[] bytes = new byte[100_000];
         LineRecord[] records = new LineRecord[10_000];
         var result = await inputProcessor.ReadRecords(path, bytes, records);

         var first = records[0];

         Assert.True(result.Success);
      }

      [Fact]
      public async Task CanSort()
      {
         byte[] bytes = new byte[2_000_000_000];
         LineRecord[] records = new LineRecord[100000];

         InputProcessor inputProcessor = new InputProcessor();
         string fileName = "fourth";
         string path = @$"d://temp/ATT/{fileName}.txt";
         Result result = await inputProcessor.ReadRecords(path, bytes, records);

         Stopwatch stopwatch = Stopwatch.StartNew();
         RecordsSorter sorter = new RecordsSorter();
         var sorted = sorter.Sort(records);

         stopwatch.Stop();

         LineAsString[] originalRecords = ConvertToStrings(records);
         LineAsString[] sortedRecords = ConvertToStrings(sorted);

         var min = stopwatch.Elapsed.Minutes;
         var sec = stopwatch.Elapsed.Seconds;
         var total = stopwatch.Elapsed.TotalMilliseconds;

         Assert.Equal(sorted.Length, records.Length);
      }

      // [Fact]
      // public async Task CanSortOnSite()
      // {
      //    byte[] bytes = new byte[2_000_000_000];
      //    LineMemory[] records = new LineMemory[100000];
      //
      //    InputProcessor inputProcessor = new InputProcessor();
      //    string fileName = "fourth";
      //    string path = @$"d://temp/ATT/{fileName}.txt";
      //    var result = await inputProcessor.ReadMemoryRecords(path, bytes, records);
      //
      //    Stopwatch stopwatch = Stopwatch.StartNew();
      //    InSiteRecordsSorter sorter = new InSiteRecordsSorter(bytes);
      //    var sorted = sorter.Sort(records);
      //
      //    stopwatch.Stop();
      //
      //    LineAsString[] originalRecords = ConvertToStrings(records, bytes);
      //    LineAsString[] sortedRecords = ConvertToStrings(sorted, bytes);
      //
      //    var min = stopwatch.Elapsed.Minutes;
      //    var sec = stopwatch.Elapsed.Seconds;
      //    var total = stopwatch.Elapsed.TotalMilliseconds;
      //
      //    Assert.Equal(sorted.Length, records.Length);
      // }

      private LineAsString[] ConvertToStrings(LineMemory[] lineRecords, byte[] source)
      {
         Encoding encoding = Encoding.UTF8;

         LineAsString[] result = new LineAsString[lineRecords.Length];
         for (int i = 0; i < lineRecords.Length; i++)
         {
            result[i] = new LineAsString(lineRecords[i].Number,
               encoding.GetString(source[lineRecords[i].From..lineRecords[i].To]));
         }
         return result;

      }

      private LineAsString[] ConvertToStrings(LineRecord[] lineRecords)
      {
         Encoding encoding = Encoding.UTF8;

         LineAsString[] result = new LineAsString[lineRecords.Length];
         for (int i = 0; i < lineRecords.Length; i++)
         {
            result[i] = new LineAsString(lineRecords[i].Number, encoding.GetString(lineRecords[i].Text));
         }
         return result;
      }

      [Fact]
      public void StringTest()
      {
         int i=12;
         string result = i.ToString("D4");
         string d = result;

         string fileName = @"d://temp/ATT/second.txt";
         string path1 = Path.GetDirectoryName(fileName);
         var file= Path.GetFileNameWithoutExtension(fileName);
         string dirName = $"{file}_{Guid.NewGuid()}";
         ReadOnlySpan<char> path2 = Path.GetDirectoryName(@"d://temp/ATT/".AsSpan());
         var path3 = Path.GetDirectoryName(@"d://temp/ATT".AsSpan());
         var path4 = Path.GetDirectoryName(@"d://temp/ATT/second".AsSpan());

      }

      [Fact]
      public void MultipleReturn()
      {
         var buffer = ArrayPool<int>.Shared.Rent(5);
         for (int i = 0; i < 5; i++)
         {
            buffer[i] = i;
         }
         var result = buffer.ToArray();

         ArrayPool<int>.Shared.Return(buffer);
         ArrayPool<int>.Shared.Return(buffer);
         ArrayPool<int>.Shared.Return(buffer);

         Assert.Equal(result[4], 4);
      }
   }

   public class RxTests
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
   public class NumbersObservable : IObservable<int>
   {
      private readonly int _amount;

      public NumbersObservable(int amount)
      {
         _amount = amount;
      }

      public IDisposable Subscribe(IObserver<int> observer)
      {
         for (int i = 0; i < _amount; i++)
         {
            observer.OnNext(i);
         }
         observer.OnCompleted();
         return Disposable.Empty;
      }
   }
   public class ConsoleObserver<T> : IObserver<T>
   {
      private readonly string _name;

      public ConsoleObserver(string name = "")
      {
         _name = name;
      }

      public void OnNext(T value)
      {
         Console.WriteLine("{0} - OnNext({1})", _name, value);
      }

      public void OnError(Exception error)
      {
         Console.WriteLine("{0} - OnError:", _name);
         Console.WriteLine("\t {0}", error);
      }

      public void OnCompleted()
      {
         Console.WriteLine("{0} - OnCompleted()", _name);
      }
   }
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
                    Console.WriteLine("\t {0}", ex);
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
}