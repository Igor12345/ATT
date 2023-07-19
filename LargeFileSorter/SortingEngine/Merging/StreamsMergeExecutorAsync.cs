using Infrastructure.Parameters;
using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.Sorting;

namespace SortingEngine.Merging;

public sealed class StreamsMergeExecutorAsync
{
   private readonly IConfig _config;
   private readonly ISeveralTimesLinesWriter _linesWriter;
   private string[] _files = null!;

   private Line[] _outputBuffer = null!;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   
   public StreamsMergeExecutorAsync(IConfig config, ISeveralTimesLinesWriter linesWriter)
   {
      _config = Guard.NotNull(config);
      _linesWriter = Guard.NotNull(linesWriter);
   }

   //todo file system dependence
   public async Task<Result> MergeWithOrderAsync()
   {
      Initialize();
      return await ExecuteMerge();
   }

   private void Initialize()
   {
      _files = Directory.GetFiles(_config.TemporaryFolder);
      _inputBuffer = new byte[_config.MergeBufferLength * _files.Length].AsMemory();
      _outputBuffer = new Line[_config.OutputBufferLength];
   }

   private async Task<Result> ExecuteMerge()
   {
      DataChunkManagerAsync[] managers = CreateDataChunkManagers();

      var queue = new IndexPriorityQueue<Line, IComparer<Line>>(_files.Length,
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
      return _lastLine == 0 ? Result.Ok : WriteLinesFromBuffer(_outputBuffer, _lastLine, _inputBuffer);
   }

   private DataChunkManagerAsync[] CreateDataChunkManagers()
   {
      DataChunkManagerAsync[] managers = new DataChunkManagerAsync[_files.Length];
      for (int i = 0; i < _files.Length; i++)
      {
         int from = i * _config.MergeBufferLength;
         int to = (i + 1) * _config.MergeBufferLength;
         managers[i] = new DataChunkManagerAsync(_files[i], _inputBuffer[from..to], _config.Encoding,
            _config.RecordsBufferLength, from);
      }

      return managers;
   }

   private async Task<Result> InitializeQueue(DataChunkManagerAsync[] managers,
      IndexPriorityQueue<Line, IComparer<Line>> queue)
   {
      for (int i = 0; i < _files.Length; i++)
      {
         (ExtractionResult extractionResult, bool hasLine, Line line) = await managers[i].TryGetNextLineAsync();
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

   private Result WriteLinesFromBuffer(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      return _linesWriter.WriteRecords(lines, linesNumber, source);
   }
}