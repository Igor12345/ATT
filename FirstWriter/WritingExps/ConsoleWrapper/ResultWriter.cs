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

internal class ResultWriter : IAsyncDisposable
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
      return await _writer.WriteRecordsAsync(records, eventArgs.LinesNumber, sourceBytes, _token);
   }

   public ValueTask DisposeAsync()
   {
      return _writer.DisposeAsync();
   }

   
}