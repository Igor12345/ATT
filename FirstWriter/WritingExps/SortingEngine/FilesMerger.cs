using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine;

internal class FilesMerger
{
   private readonly IConfig _config;
   private string[] _files;

   private LineMemory[] _outputBuffer;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   public event EventHandler<SortingCompletedEventArgs> SortingCompleted;
   public FilesMerger(IConfig config)
   {
      //todo static vs Guard
      _config = config ?? throw new ArgumentNullException(nameof(config));
   }

   public async Task<Result> MergeWithOrder(string directory)
   {
      _files = Directory.GetFiles(directory);

      CreateBuffers();

      return await ExecuteMerge();
   }

   private async Task<Result> ExecuteMerge()
   {
      DataChunkManager[] managers = new DataChunkManager[_files.Length];
      for (int i = 0; i < _files.Length; i++)
      {
         //todo configure LineMemory creation
         int from = i * _config.MergeBufferSize;
         int to = (i + 1) * _config.MergeBufferSize;
         managers[i] = new DataChunkManager(_files[i], _inputBuffer[from..to], new LineMemory[1000]);
      }

      //todo replace on real storage
      PriorityQueue<LineMemory, LineMemory> priorityQueue =
         new PriorityQueue<LineMemory, LineMemory>(new InSiteRecordsComparer(_inputBuffer));


      for (int i = 0; i < _files.Length; i++)
      {
         LineMemory line = await managers[i].GetNextLineAsync();
         priorityQueue.Enqueue(line, line);
      }

      while (true)
      {
         if (priorityQueue.Count == 0)
            break;
         var line = priorityQueue.Dequeue();
         _outputBuffer[_lastLine++] = line;
         if (_lastLine >= _outputBuffer.Length)
         {
            OnSortingCompleted(new SortingCompletedEventArgs(_outputBuffer, _inputBuffer));
            _lastLine = 0;
         }
      }
      OnSortingCompleted(new SortingCompletedEventArgs(_outputBuffer[.._lastLine], _inputBuffer));
      //todo flush output
      return Result.Ok;
   }



   private void OnSortingCompleted(SortingCompletedEventArgs e)
   {
      SortingCompleted?.Invoke(this, e);
   }

   private void CreateBuffers()
   {
      _inputBuffer = new byte[_config.MergeBufferSize * _files.Length].AsMemory();
      _outputBuffer = new LineMemory[_config.OutputBufferSize];
   }
}