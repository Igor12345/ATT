using ConsoleWrapper.IOProcessing;
using Infrastructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper;

internal class ResultWriter : IAsyncDisposable
{
   private readonly CancellationToken _token;
   private RecordsWriter _writer = null!;

   private ResultWriter(CancellationToken token)
   {
      _token = token;
   }

   public static ResultWriter Create(string path, CancellationToken token)
   {
      path = Guard.FileExist(path);
      var fileName = Path.GetFileNameWithoutExtension(path);
      var extension = Path.GetExtension(path);
      string delimiter = extension.Length > 0 ? "." : "";
      string resultFile = $"{fileName}_sorted{delimiter}{extension}";
      //todo
      string? directory = Path.GetDirectoryName(path);
      string pathToResult = Path.Combine(directory, resultFile);
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
}