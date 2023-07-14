using System.Reactive.Subjects;
using ConsoleWrapper.IOProcessing;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace ConsoleWrapper;

internal class ResultWriter : IAsyncDisposable, IAsyncObserver<AfterSortingPhasePackage>
{
   private readonly CancellationToken _token;
   private RecordsWriter _writer = null!;
   private object _lock = new();

   private readonly HashSet<int> _processedPackages = new();
   private volatile int _lastPackageNumber = -1;
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
      LineMemory[] records = eventArgs.Sorted;
      ReadOnlyMemory<byte> sourceBytes = eventArgs.Source;
      return await _writer.WriteRecords(records, eventArgs.LinesNumber, sourceBytes, _token);
   }

   public ValueTask DisposeAsync()
   {
      return _writer.DisposeAsync();
   }

   private readonly SimpleAsyncSubject<AfterSortingPhasePackage> _sortedLinesSavedSubject =
      new SequentialSimpleAsyncSubject<AfterSortingPhasePackage>();

   public IAsyncObservable<AfterSortingPhasePackage> SortedLinesSaved => _sortedLinesSavedSubject;

   public async ValueTask OnNextAsync(AfterSortingPhasePackage package)
   {
      var result = await _writer.WriteRecords(package.SortedLines, package.LinesNumber, package.RowData, _token);

      if (!result.Success)
         await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

      await _sortedLinesSavedSubject.OnNextAsync(package);

      bool allProcessed = true;
      lock (_lock)
      {
         _processedPackages.Add(package.PackageNumber);
         if (package.IsLastPackage)
            _lastPackageNumber = package.PackageNumber;
         if (package.PackageNumber <= _lastPackageNumber)
         {
            for (int i = 0; i < _lastPackageNumber; i++)
            {
               allProcessed &= _processedPackages.Contains(i);
            }
         }
      }

      if (allProcessed)
         await _sortedLinesSavedSubject.OnCompletedAsync();
   }

   public async ValueTask OnErrorAsync(Exception error)
   {
      await _sortedLinesSavedSubject.OnCompletedAsync();
   }

   public ValueTask OnCompletedAsync()
   {
      return ValueTask.CompletedTask;
   }
}