using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ConsoleWrapper.IOProcessing;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace ConsoleWrapper;

internal class ResultWriter : IAsyncDisposable, IAsyncObserver<AfterSortingPhasePackage>
{
   private readonly Logger _logger;
   private readonly CancellationToken _token;
   private RecordsWriter _writer = null!;
   private object _lock = new();

   private readonly HashSet<int> _processedPackages = new();
   private volatile int _lastPackageNumber = -1;
   private ResultWriter(Logger logger, CancellationToken token)
   {
      _logger = Guard.NotNull(logger);
      _token = Guard.NotNull(token);
   }

   //todo
   // [Conditional("Verbose")]
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }

   public static ResultWriter Create(string pathToResult, Logger logger, CancellationToken token)
   {
      ResultWriter instance = new ResultWriter(logger, token)
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
      await Log(
         $"Processing package: {package.PackageNumber}(last - {package.IsLastPackage}), Lines: {package.LinesNumber}, bytes: {package.RowData.Length}, AllLines: {package.SortedLines}");
      
      var result = await _writer.WriteRecords(package.SortedLines, package.LinesNumber, package.RowData, _token);

      if (!result.Success)
         await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

      await Log($"Processed package: {package.PackageNumber}, all lines saved: {result.Success}");
      
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
      await Log($"Processed package: {package.PackageNumber}, ready to complete: {allProcessed}");
      
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