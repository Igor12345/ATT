using System.Reactive.Subjects;
using Infrastructure.Parameters;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.Sorting;

internal class IntermediateResultsDirector 
{
   private readonly IOneTimeLinesWriter _linesWriter;
   private readonly IConfig _configuration;
   private volatile int _lastFileNumber;
   private readonly string _path;
   private readonly string? _filePath;
   private readonly object _lock = new();
   private readonly HashSet<int> _processedPackages = new();
   private volatile int _lastPackageNumber = -1;

   private IntermediateResultsDirector(IOneTimeLinesWriter linesWriter, IConfig configuration)
   {
      _linesWriter = Guard.NotNull(linesWriter);
      _configuration = Guard.NotNull(configuration);
      _path = Guard.NotNullOrEmpty(configuration.TemporaryFolder);
      //use strategy
      if (_configuration.UseOneWay)
         _filePath = _configuration.Output;
   }

   public static IntermediateResultsDirector Create(IOneTimeLinesWriter linesWriter, IConfig configuration)
   {
      IntermediateResultsDirector instance = new IntermediateResultsDirector(linesWriter, configuration);
      if (!configuration.UseOneWay)
         instance.InitTemporaryFolder();
      return instance;
   }

   private void InitTemporaryFolder()
   {
      if (!Directory.Exists(_path))
         Directory.CreateDirectory(_path);
   }

   private Result WriteRecords(AfterSortingPhasePackage package)
   {
      if (package.LinesNumber == 0)
         return Result.Ok;

      string fileName = GetNextFileName();
      string filePath = (_configuration.UseOneWay ? _filePath : Path.Combine(_path, fileName))!;

      return _linesWriter.WriteRecords(filePath, package.SortedLines, package.LinesNumber, package.RowData);
   }

   private string GetNextFileName()
   {
      int currentNumber = Interlocked.Increment(ref _lastFileNumber);
      return currentNumber.ToString("D5");
   }

   private readonly IAsyncSubject<AfterSortingPhasePackage> _sortedLinesSavedSubject =
      new ConcurrentSimpleAsyncSubject<AfterSortingPhasePackage>();

   public IAsyncObservable<AfterSortingPhasePackage> SortedLinesSaved => _sortedLinesSavedSubject;

   public async ValueTask<AfterSortingPhasePackage> ProcessPackageAsync(AfterSortingPhasePackage package)
   {
      Result result = WriteRecords(package);
      if (!result.Success)
         await _sortedLinesSavedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

      await _sortedLinesSavedSubject.OnNextAsync(package);

      bool allProcessed = CheckIfAllProcessed(package);

      if (allProcessed)
         await _sortedLinesSavedSubject.OnCompletedAsync();

      return package;
   }

   private bool CheckIfAllProcessed(AfterSortingPhasePackage package)
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
      return allProcessed;
   }
}