using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngine.RuntimeConfiguration;
using Infrastructure.Parameters;
using SortingEngine.RowData;
using SortingEngine.Sorting;

namespace SortingEngine;

public sealed class StreamsMergeExecutor
{
   private readonly IConfig _config;
   private readonly ILinesWriter _linesWriter;
   private string[] _files = null!;

   private LineMemory[] _outputBuffer = null!;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   
   public StreamsMergeExecutor(IConfig config, ILinesWriter linesWriter)
   {
      _config = Guard.NotNull(config);
      _linesWriter = Guard.NotNull(linesWriter);
   }

   //todo file system dependence
   public async Task<Result> MergeWithOrder()
   {
      Initialize();
      return await ExecuteMerge();
   }

   private void Initialize()
   {
      _files = Directory.GetFiles(_config.TemporaryFolder);
      _inputBuffer = new byte[_config.MergeBufferLength * _files.Length].AsMemory();
      _outputBuffer = new LineMemory[_config.OutputBufferLength];
   }

   private async Task<Result> ExecuteMerge()
   {
      DataChunkManager[] managers = CreateDataChunkManagers();

      var queue = new IndexPriorityQueue<LineMemory, IComparer<LineMemory>>(_files.Length,
            new OnSiteLinesComparer(_inputBuffer));
      Result creatingQueueResult = await InitializeQueue(managers, queue);
      if (!creatingQueueResult.Success)
         return creatingQueueResult;
      
      while (queue.Any())
      {
         var (line, streamIndex) = queue.Dequeue();
         var (extractionResult, lineAvailable, nextLine) = await managers[streamIndex].TryGetNextLineAsync();
         if(!extractionResult.Success)
            return Result.Error(extractionResult.Message);
         
         if (lineAvailable)
            queue.Enqueue(nextLine, streamIndex);

         _outputBuffer[_lastLine++] = line;
         if (_lastLine >= _outputBuffer.Length)
         {
            Result writingResult = WriteLinesFromBuffer(_outputBuffer, _outputBuffer.Length, _inputBuffer);
            if (!writingResult.Success)
               return writingResult;
            _lastLine = 0;
         }
      }
      //todo !!! add eol if necessary
      return _lastLine == 0 ? Result.Ok : WriteLinesFromBuffer(_outputBuffer, _lastLine, _inputBuffer);
   }

   private DataChunkManager[] CreateDataChunkManagers()
   {
      DataChunkManager[] managers = new DataChunkManager[_files.Length];
      for (int i = 0; i < _files.Length; i++)
      {
         //todo configure LineMemory creation
         int from = i * _config.MergeBufferLength;
         int to = (i + 1) * _config.MergeBufferLength;
         managers[i] = new DataChunkManager(_files[i], _inputBuffer[from..to], _config.Encoding,
            _config.RecordsBufferLength, from);
      }

      return managers;
   }

   private async Task<Result> InitializeQueue(DataChunkManager[] managers,
      IndexPriorityQueue<LineMemory, IComparer<LineMemory>> queue)
   {
      for (int i = 0; i < _files.Length; i++)
      {
         (ExtractionResult extractionResult, bool hasLine, LineMemory line) = await managers[i].TryGetNextLineAsync();
         if (!extractionResult.Success)
         {
            return Result.Error(extractionResult.Message);
         }

         if (hasLine)
         {
            queue.Enqueue(line, i);
         }
      }

      return Result.Ok;
   }

   private Result WriteLinesFromBuffer(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      return _linesWriter.WriteRecords(lines, linesNumber, source);
   }
}