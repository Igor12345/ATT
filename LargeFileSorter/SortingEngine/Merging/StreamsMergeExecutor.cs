using LogsHub;
using SortingEngine.Algorithms;
using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.Merging;

public sealed class StreamsMergeExecutor :IDisposable
{
   private readonly Func<string, Stream> _dataStreamFactory;
   private readonly ILogger _logger;
   private readonly IConfig _config;
   private readonly ISeveralTimesLinesWriter _linesWriter;
   private string[] _files = null!;

   private Line[] _outputBuffer = null!;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   private DataChunkManager[]? _managers;
   private int _reported;

   public StreamsMergeExecutor(IConfig config, ISeveralTimesLinesWriter linesWriter,
      Func<string, Stream> dataStreamFactory, ILogger logger)
   {
      _config = NotNull(config);
      _linesWriter = NotNull(linesWriter);
      _dataStreamFactory = NotNull(dataStreamFactory);
      _logger = NotNull(logger);
   }

   //todo file system dependence
   public Result MergeWithOrder()
   {
      Initialize();
      if(!_files.Any())
         return Result.Ok;
      
      return  ExecuteMerge();
   }

   private void Initialize()
   {
      //todo dependency on the file system, can be removed
      _files = Directory.GetFiles(_config.TemporaryFolder);
      _inputBuffer = new byte[_config.MergeBufferLength * _files.Length].AsMemory();
      _outputBuffer = new Line[_config.OutputBufferLength];
   }

   private DataChunkManager[] CreateDataChunkManagers()
   {
      DataChunkManager[] managers = new DataChunkManager[_files.Length];
      //KmpMatcher can be a Singleton. Work for DI
      LineParser parser = new LineParser(KmpMatcher.CreateForThisPattern(_config.DelimiterBytes), _config.Encoding);
      IParticularSubstringMatcher eolFinder = KmpMatcher.CreateForThisPattern(_config.EolBytes);
      LinesExtractor extractor = new LinesExtractor(eolFinder, _config.EolBytes.Length, parser);
      Func<Result> flushOutputBuffer = () => FlushOutputBuffer();
      for (int i = 0; i < _files.Length; i++)
      {
         int from = i * _config.MergeBufferLength;
         int to = (i + 1) * _config.MergeBufferLength;
         int j = i;
         managers[i] = new DataChunkManager(() => _dataStreamFactory(_files[j]), _inputBuffer[from..to], from,
            extractor, new ExpandingStorage<Line>(_config.RecordsBufferLength), _config.MaxLineLength,
            flushOutputBuffer);
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

   private Result ExecuteMerge()
   {
      _managers = CreateDataChunkManagers();

      var queue = new IndexPriorityQueue<Line, IComparer<Line>>(_files.Length,
            new OnSiteLinesComparer(_inputBuffer));
      Result creatingQueueResult = InitializeQueue(_managers, queue);
      if (!creatingQueueResult.Success)
         return creatingQueueResult;
      
      while (queue.Any())
      {
         var (line, streamIndex) = queue.Dequeue();
         var (extractionResult, lineAvailable, nextLine) = _managers[streamIndex].TryGetNextLine();
         if(!extractionResult.Success)
            return Result.Error(extractionResult.Message);
         
         if (lineAvailable)
            queue.Enqueue(nextLine, streamIndex);

         _outputBuffer[_lastLine++] = line;
         if (_lastLine >= _outputBuffer.Length)
         {
            Result writingResult = FlushOutputBuffer();
            if (!writingResult.Success)
               return writingResult;
         }
      }
      
      return _lastLine == 0 ? Result.Ok : FlushOutputBuffer();
   }

   private Result FlushOutputBuffer()
   {
      Result result = _linesWriter.WriteLines(_outputBuffer, _lastLine, _inputBuffer);
      ReportProgress(_lastLine);
      _lastLine = 0;
      return result;
   }

   private void ReportProgress(int lines)
   {
      _reported += lines;
      if (_reported > 500_000)
      {
         _logger.Log($"{DateTime.Now:hh:mm:ss-fff}: Next {_reported} lines has been added to the file.");
         _reported = 0;
      }
   }

   public void Dispose()
   {
      if(_managers!=null)
         foreach (DataChunkManager manager in _managers)
         {
            manager.Dispose();
         }

      if (_config.CleanUp)
      {
         Directory.Delete(_config.TemporaryFolder, true);
      }
   }
}