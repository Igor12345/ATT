using Infrastructure.Parameters;
using SortingEngine.Algorithms;
using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.Merging;

public sealed class StreamsMergeExecutor
{
   private readonly IConfig _config;
   private readonly ISeveralTimesLinesWriter _linesWriter;
   private string[] _files = null!;

   private Line[] _outputBuffer = null!;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   
   public StreamsMergeExecutor(IConfig config, ISeveralTimesLinesWriter linesWriter)
   {
      _config = Guard.NotNull(config);
      _linesWriter = Guard.NotNull(linesWriter);
   }

   //todo file system dependence
   public Result MergeWithOrder()
   {
      Initialize();
      return  ExecuteMerge();
   }

   private void Initialize()
   {
      _files = Directory.GetFiles(_config.TemporaryFolder);
      _inputBuffer = new byte[_config.MergeBufferLength * _files.Length].AsMemory();
      _outputBuffer = new Line[_config.OutputBufferLength];
   }

   private Result ExecuteMerge()
   {
      DataChunkManager[] managers = CreateDataChunkManagers();

      var queue = new IndexPriorityQueue<Line, IComparer<Line>>(_files.Length,
            new OnSiteLinesComparer(_inputBuffer));
      Result creatingQueueResult = InitializeQueue(managers, queue);
      if (!creatingQueueResult.Success)
         return creatingQueueResult;
      
      while (queue.Any())
      {
         var (line, streamIndex) = queue.Dequeue();
         var (extractionResult, lineAvailable, nextLine) = managers[streamIndex].TryGetNextLine();
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

   private DataChunkManager[] CreateDataChunkManagers()
   {
      DataChunkManager[] managers = new DataChunkManager[_files.Length];
      //KmpMatcher can be a Singleton. Work for DI
      LineParser parser = new LineParser(KmpMatcher.CreateForPattern(_config.DelimiterBytes), _config.Encoding);
      LinesExtractor extractor = new LinesExtractor(_config.EolBytes, parser);
      for (int i = 0; i < _files.Length; i++)
      {
         int from = i * _config.MergeBufferLength;
         int to = (i + 1) * _config.MergeBufferLength;
         managers[i] = new DataChunkManager(_files[i], _inputBuffer[from..to], from, extractor, 
            () => new ExpandingStorage<Line>(_config.RecordsBufferLength), _config.MaxLineLength);
      }

      return managers;
   }

   private Result InitializeQueue(DataChunkManager[] managers,
      IndexPriorityQueue<Line, IComparer<Line>> queue)
   {
      for (int i = 0; i < _files.Length; i++)
      {
         (ExtractionResult extractionResult, bool hasLine, Line line) = managers[i].TryGetNextLine();
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
      return _linesWriter.WriteLines(lines, linesNumber, source);
   }
}