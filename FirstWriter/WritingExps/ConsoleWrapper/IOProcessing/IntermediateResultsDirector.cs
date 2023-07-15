﻿using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace ConsoleWrapper.IOProcessing;

internal class IntermediateResultsDirector: IAsyncObserver<AfterSortingPhasePackage>
{
   private readonly IConfig _configuration;
   private readonly ILogger _logger;
   private volatile int _lastFileNumber;
   private readonly string _path;
   private readonly CancellationToken _token;
   private readonly object _lock = new();
   private readonly HashSet<int> _processedPackages = new();
   private volatile int _lastPackageNumber = -1;

   private IntermediateResultsDirector(IConfig configuration, ILogger logger, CancellationToken token)
   {
      _configuration = Guard.NotNull(configuration);
      _path = Guard.NotNullOrEmpty(configuration.TemporaryFolder);
      _logger = Guard.NotNull(logger);
      _token = Guard.NotNull(token);
   }

   public static IntermediateResultsDirector Create(IConfig configuration, ILogger logger, CancellationToken token = default)
   {
      IntermediateResultsDirector instance = new IntermediateResultsDirector(configuration, logger, token);
      instance.Init(configuration.TemporaryFolder);
      return instance;
   }

   private void Init(string path)
   {
      if (!Directory.Exists(_path))
         Directory.CreateDirectory(_path);
   }

   public Result WriteRecords(SortingCompletedEventArgs eventArgs)
   {
      var records = eventArgs.Sorted;
      var sourceBytes = eventArgs.Source;

      string fileName = GetNextFileName();
      var fullFileName = Path.Combine(_path, fileName);

      using RecordsWriter writer = RecordsWriter.Create(fullFileName, _configuration.Encoding.GetBytes(".").Length, _logger);
      return writer.WriteRecords(records, eventArgs.LinesNumber, sourceBytes);
   }
   
   private Result WriteRecords(AfterSortingPhasePackage package)
   {
      if(package.LinesNumber==0)
         return Result.Ok;
      
      string fileName = GetNextFileName();
      string fullFileName = Path.Combine(_path, fileName);

      //use synchronous version to prevent from holding the variable by async state machine
      //it looks like something wrong this this version of async code
      using RecordsWriter writer = RecordsWriter.Create(fullFileName, _configuration.Encoding.GetBytes(".").Length, _logger);
      return writer.WriteRecords(package.SortedLines, package.LinesNumber, package.RowData);
   }

   private string GetNextFileName()
   {
      int currentNumber = Interlocked.Increment(ref _lastFileNumber);
      return currentNumber.ToString("D5");
   }

   private readonly IAsyncSubject<AfterSortingPhasePackage> _sortedLinesSavedSubject =
      new ConcurrentSimpleAsyncSubject<AfterSortingPhasePackage>();

   public IAsyncObservable<AfterSortingPhasePackage> SortedLinesSaved => _sortedLinesSavedSubject;

   public async ValueTask ProcessPackage(AfterSortingPhasePackage package)
   {
      await Log(
         $"Processing package: {package.PackageNumber}(last - {package.IsLastPackage}), " +
         $"Lines: {package.LinesNumber}, bytes: {package.RowData.Length},AllLines: {package.SortedLines} ");

      Result result = WriteRecords(package);
      if (!result.Success)
         await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

      await Log($"Processed package: {package.PackageNumber}, all lines saved: {result.Success}");

      await _sortedLinesSavedSubject.OnNextAsync(package);

      bool allProcessed = await CheckIfAllProcessed(package);

      if (allProcessed)
      {
         Console.WriteLine(
            $"All packages processed after {package.PackageNumber}, thread {Thread.CurrentThread.ManagedThreadId}");
         await _sortedLinesSavedSubject.OnCompletedAsync();
      }
   }

   public async ValueTask OnNextAsync(AfterSortingPhasePackage inputPackage)
   {
      await Log(
         $"Processing package: {inputPackage.PackageNumber}(last - {inputPackage.IsLastPackage}), " +
         $"Lines: {inputPackage.LinesNumber}, bytes: {inputPackage.RowData.Length},AllLines: {inputPackage.SortedLines} ");

      await Task.Factory.StartNew<Task<bool>>(async (state) =>
         {
            if (state == null) throw new ArgumentNullException(nameof(state));
            AfterSortingPhasePackage package = (AfterSortingPhasePackage)state;

            Result result = WriteRecords(package);
            if (!result.Success)
               await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

            await Log($"Processed package: {package.PackageNumber}, all lines saved: {result.Success}");

            await _sortedLinesSavedSubject.OnNextAsync(package);

            bool allProcessed = await CheckIfAllProcessed(package);

            if (allProcessed)
            {
               Console.WriteLine($"All packages processed after {package.PackageNumber}, thread {Thread.CurrentThread.ManagedThreadId}");
               await _sortedLinesSavedSubject.OnCompletedAsync();
            }

            return true;
         }, inputPackage, _token,
         TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default);
   }

   private async Task<bool> CheckIfAllProcessed(AfterSortingPhasePackage package)
   {
      bool allProcessed = false;
      lock (_lock)
      {
         _processedPackages.Add(package.PackageNumber);
         if (package.IsLastPackage)
            _lastPackageNumber = package.PackageNumber;
         
         if (package.PackageNumber <= _lastPackageNumber)
         {
            allProcessed = true;
            for (int i = 0; i < _lastPackageNumber; i++)
            {
               allProcessed &= _processedPackages.Contains(i);
            }
         }
      }
      
      await Log($"Processed package: {package.PackageNumber}, ready to complete: {allProcessed}");
      return allProcessed;
   }

   public async ValueTask OnErrorAsync(Exception error)
   {
      await _sortedLinesSavedSubject.OnCompletedAsync();
   }

   public ValueTask OnCompletedAsync()
   {
      return ValueTask.CompletedTask;
   }
   
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }
}