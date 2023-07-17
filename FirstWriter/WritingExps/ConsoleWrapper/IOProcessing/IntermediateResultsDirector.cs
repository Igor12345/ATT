using System.Reactive.Subjects;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace ConsoleWrapper.IOProcessing;

internal class IntermediateResultsDirector //: IAsyncObserver<AfterSortingPhasePackage>
{
   private readonly IConfig _configuration;
   private readonly ILogger _logger;
   private volatile int _lastFileNumber;
   private readonly string _path;
   private readonly string _filePath;
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
      //use strategy
      if (_configuration.UseOneWay)
         _filePath = _configuration.Output;
   }

   public static IntermediateResultsDirector Create(IConfig configuration, ILogger logger, CancellationToken token = default)
   {
      IntermediateResultsDirector instance = new IntermediateResultsDirector(configuration, logger, token);
      if (!configuration.UseOneWay)
         instance.InitTemporaryFolder(configuration.TemporaryFolder);
      return instance;
   }

   private void InitTemporaryFolder(string path)
   {
      if (!Directory.Exists(_path))
         Directory.CreateDirectory(_path);
   }

   private Result WriteRecords(AfterSortingPhasePackage package)
   {
      if (package.LinesNumber == 0)
         return Result.Ok;

      string fileName = GetNextFileName();
      string fullFileName = _configuration.UseOneWay ? _filePath : Path.Combine(_path, fileName);

      //todo
      //use synchronous version to prevent from holding the variable by async state machine
      //it looks like something wrong this this version of async code
      using RecordsWriter writer =
         RecordsWriter.Create(fullFileName, _configuration.Encoding.GetBytes("1").Length, _logger);
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

   public async ValueTask<AfterSortingPhasePackage> ProcessPackage(AfterSortingPhasePackage package)
   {
      int id = package.RowData.GetHashCode();
      await Log(
         $"Processing package: {package.PackageNumber}(last - {package.IsLastPackage}), " +
         $"Before write this chunk on Disk: Lines: {package.LinesNumber}, bytes: {package.RowData.Length}, buffer Id: {id}, AllLines: {package.SortedLines}, thread: {Thread.CurrentThread.ManagedThreadId}  ");

      Result result = WriteRecords(package);
      if (!result.Success)
         await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

      await Log($"Processed package: {package.PackageNumber}, all lines saved: {result.Success}");

      //todo
      // id = package.RowData.GetHashCode();
      // Console.WriteLine($"Sending IntermediateResultsDirector.SortedLinesSaved.OnNextAsync {package.PackageNumber}, is last: {package.IsLastPackage}, buffer Id: {id}");
      await _sortedLinesSavedSubject.OnNextAsync(package);

      bool allProcessed = await CheckIfAllProcessed(package);

      if (allProcessed)
      {
         // Console.WriteLine(
         //    $"<____!!!_____> All packages processed after {package.PackageNumber}, closing SortedLinesSaved !!!, thread {Thread.CurrentThread.ManagedThreadId}");
         await _sortedLinesSavedSubject.OnCompletedAsync();
      }

      return package;
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
   
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }
}