using System.Reactive.Subjects;
using ConsoleWrapper.IOProcessing;
using Infrastructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper;

internal class ResultWriter : IAsyncDisposable, IAsyncObserver<SortingCompletedEventArgs>
{
   private readonly CancellationToken _token;
   private RecordsWriter _writer = null!;

   private ResultWriter(CancellationToken token)
   {
      _token = token;
   }

   public static ResultWriter Create(string pathToResult, CancellationToken token)
   {
      ResultWriter instance = new ResultWriter(token)
      {
         _writer = RecordsWriter.Create(pathToResult)
      };
      return instance;
   }

   public Result WriteOutput(SortingCompletedEventArgs eventArgs)
   {
      return WriteOutputAsync(eventArgs).GetAwaiter().GetResult();
   }

   public async Task<Result> WriteOutputAsync(SortingCompletedEventArgs eventArgs)
   {
      var records = eventArgs.Sorted;
      var sourceBytes = eventArgs.Source;
      return await _writer.WriteRecords(records, sourceBytes, _token);
   }

   public ValueTask DisposeAsync()
   {
      return _writer.DisposeAsync();
   }

   private readonly SimpleAsyncSubject<SortingCompletedEventArgs> _sortedLinesSavedSubject =
      new SequentialSimpleAsyncSubject<SortingCompletedEventArgs>();

   public IAsyncObservable<SortingCompletedEventArgs> SortedLinesSaved => _sortedLinesSavedSubject;

   public async ValueTask OnNextAsync(SortingCompletedEventArgs value)
   {
      var result = await WriteOutputAsync(value);
      if (!result.Success)
         await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));
      await _sortedLinesSavedSubject.OnNextAsync(value);
   }

   public async ValueTask OnErrorAsync(Exception error)
   {
      await _sortedLinesSavedSubject.OnCompletedAsync();
   }

   public async ValueTask OnCompletedAsync()
   {
      await _sortedLinesSavedSubject.OnCompletedAsync();
   }
}