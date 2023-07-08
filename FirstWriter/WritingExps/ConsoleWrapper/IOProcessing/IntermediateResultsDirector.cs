using Infrastructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing;

internal class IntermediateResultsDirector
{
   private volatile int _lastFileNumber;
   private string _path = null!;
   private readonly RecordsWriter _writer;
   private CancellationToken _cancellationToken;

   private IntermediateResultsDirector()
   {
      _writer = new RecordsWriter();
   }

   public static IntermediateResultsDirector Create(string path, CancellationToken token = default)
   {
      IntermediateResultsDirector instance = new IntermediateResultsDirector();
      instance.Init(path, token);
      return instance;
   }

   public void Init(string path, CancellationToken cancellationToken)
   {
      _cancellationToken = cancellationToken;
      path = Guard.PathExist(path);

      var sourceDir = Path.GetDirectoryName(path.AsSpan());
      string fileName = "";
      if (File.Exists(path))
      {
         fileName = Path.GetFileNameWithoutExtension(path);
      }

      string dirName = $"{fileName}_{Guid.NewGuid()}";
      _path = Path.Combine(sourceDir.ToString(), dirName);
      Directory.CreateDirectory(_path);
   }

   public async Task<Result> WriteRecordsAsync(object? sender, SortingCompletedEventArgs eventArgs)
   {
      var records = eventArgs.Sorted;
      var sourceBytes = eventArgs.Source;

      string fileName = GetNextFileName();
      var fullFileName = Path.Combine(_path, fileName);

      return await _writer.WriteRecords(fullFileName, records, sourceBytes.AsMemory(), _cancellationToken);
   }

   private string GetNextFileName()
   {
      int currentNumber = Interlocked.Increment(ref _lastFileNumber);
      return currentNumber.ToString("D5");
   }
}