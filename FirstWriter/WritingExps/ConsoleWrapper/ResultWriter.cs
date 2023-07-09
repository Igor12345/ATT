using ConsoleWrapper.IOProcessing;
using Infrastructure.Parameters;
using SortingEngine;
using System.Threading;

namespace ConsoleWrapper;

internal class ResultWriter
{
   private string _path = null!;
   private readonly CancellationToken _token;
   private readonly RecordsWriter _writer;
   
   private ResultWriter(string path, CancellationToken token)
   {
      _path = path;
      _token = token;
      _writer = new RecordsWriter();
   }

   public static ResultWriter Create(string path, CancellationToken token)
   {
      path = Guard.FileExist(path);
      var fileName = Path.GetFileNameWithoutExtension(path);
      var extension = Path.GetExtension(path);
      string resultFile = $"{fileName}_sorted.{extension}";
      ResultWriter instance = new ResultWriter(resultFile, token);
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
      return await _writer.WriteRecords(_path, records, sourceBytes, _token);
   }
}