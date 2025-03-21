﻿using SortingEngine;

namespace ConsoleWrapper.IOProcessing;

internal class IntermediateResultsDirector
{
   private volatile int _lastFileNumber;
   private string _path = null!;
   private CancellationToken _cancellationToken;

   private IntermediateResultsDirector()
   {
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
      _path = path;
      if (!Directory.Exists(_path))
         Directory.CreateDirectory(_path);
   }

   public async Task<Result> WriteRecordsAsync(SortingCompletedEventArgs eventArgs)
   {
      var records = eventArgs.Sorted;
      var sourceBytes = eventArgs.Source;

      string fileName = GetNextFileName();
      var fullFileName = Path.Combine(_path, fileName);

      await using RecordsWriter writer = RecordsWriter.Create(fullFileName);
      return await writer.WriteRecords(records, sourceBytes, _cancellationToken);
   }

   public Result WriteRecords(SortingCompletedEventArgs eventArgs)
   {
      return WriteRecordsAsync(eventArgs).GetAwaiter().GetResult();
   }

   private string GetNextFileName()
   {
      int currentNumber = Interlocked.Increment(ref _lastFileNumber);
      return currentNumber.ToString("D5");
   }
}