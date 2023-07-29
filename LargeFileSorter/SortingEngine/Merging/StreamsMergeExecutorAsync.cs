using LogsHub;
using SortingEngine.Algorithms;
using SortingEngine.Comparators;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.Merging;

public sealed class StreamsMergeExecutorAsync
{
   private readonly Func<string, Stream> _dataStreamFactory;
   private readonly ILogger _logger;
   private readonly CancellationToken _cancellationToken;
   private readonly IConfig _config;
   private readonly ISeveralTimesLinesWriter _linesWriter;
   private string[] _files = null!;

   private Line[] _outputBuffer = null!;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   private int _reported;
   
   public StreamsMergeExecutorAsync(IConfig config, ISeveralTimesLinesWriter linesWriter,
      Func<string, Stream> dataStreamFactory, ILogger logger,
      CancellationToken cancellationToken)
   {
      _config = NotNull(config);
      _linesWriter = NotNull(linesWriter);
      _dataStreamFactory = NotNull(dataStreamFactory);
      _logger = NotNull(logger);
      _cancellationToken = NotNull(cancellationToken);
   }

   //todo file system dependence
   public async Task<Result> MergeWithOrderAsync()
   {
      Initialize();
      return await ExecuteMergeAsync();
   }

   private void Initialize()
   {
      _files = Directory.GetFiles(_config.TemporaryFolder);
      _inputBuffer = new byte[_config.MergeBufferLength * _files.Length].AsMemory();
      _outputBuffer = new Line[_config.OutputBufferLength];
   }

   private DataChunkManagerAsync[] CreateDataChunkManagers()
   {
      DataChunkManagerAsync[] managers = new DataChunkManagerAsync[_files.Length];
      //KmpMatcher can be a Singleton. Work for DI
      LineParser parser = new LineParser(KmpMatcher.CreateForThisPattern(_config.DelimiterBytes), _config.Encoding);
      IParticularSubstringMatcher eolFinder = KmpMatcher.CreateForThisPattern(_config.EolBytes);
      LinesExtractor extractor = new LinesExtractor(eolFinder, _config.EolBytes.Length, parser);

      Func<Task<Result>> flushOutputBuffer = () => FlushOutputBufferAsync();
      for (int i = 0; i < _files.Length; i++)
      {
         int from = i * _config.MergeBufferLength;
         int to = (i + 1) * _config.MergeBufferLength;
         int j = i;
         managers[i] = new DataChunkManagerAsync(() => _dataStreamFactory(_files[j]), _inputBuffer[from..to], from, extractor,
            new ExpandingStorage<Line>(_config.RecordsBufferLength), _config.MaxLineLength, flushOutputBuffer,
            _cancellationToken);
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

   private async Task<Result> ExecuteMergeAsync()
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
            Result writingResult = await FlushOutputBufferAsync();
            if (!writingResult.Success)
               return writingResult;
            _lastLine = 0;
         }
      }
      return _lastLine == 0 ? Result.Ok : await FlushOutputBufferAsync();
   }

   private async Task<Result> FlushOutputBufferAsync()
   {
      Result result = await _linesWriter.WriteLinesAsync(_outputBuffer, _lastLine, _inputBuffer, _cancellationToken);
      await ReportProgressAsync(_lastLine);
      _lastLine = 0;
      return result;
   }

   private async Task ReportProgressAsync(int lines)
   {
      _reported += lines;
      if (_reported > 500_000)
      {
         await _logger.LogAsync($"{DateTime.Now:hh:mm:ss-fff}: Next {_reported} lines has been added to the file.");
         _reported = 0;
      }
   }
}