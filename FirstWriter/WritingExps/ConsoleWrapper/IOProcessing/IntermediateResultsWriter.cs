using InfoStructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing;

internal class IntermediateResultsWriter
{
   private volatile int _lastFileNumber = 0;
   private string _path = null!;

   private IntermediateResultsWriter()
   {
   }

   public static IntermediateResultsWriter Create(string path)
   {
      IntermediateResultsWriter writer = new IntermediateResultsWriter();
      writer.Init(path);
      return writer;
   }

   public void Init(string path)
   {
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

   public async Task WriteRecordsAsync(object? sender, SortingCompletedEventArgs eventArgs)
   {
      var records = eventArgs.Sorted;
      var sourceBytes = eventArgs.Source;

      string fileName = GetNextFileName();
      var fullFileName = Path.Combine(_path, fileName);
      await using FileStream fileStream = File.Open(fullFileName, FileMode.Create, FileAccess.Write);

      for (int i = 0; i < records.Length; i++)
      {
         await fileStream.WriteAsync(sourceBytes[records[i].From..records[i].To]).ConfigureAwait(false);
      }
   }

   private string GetNextFileName()
   {
      int currentNumber = Interlocked.Increment(ref _lastFileNumber);
      return currentNumber.ToString("D5");
   }
}