using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.Sorters;
using System.Collections.Generic;
using Infrastructure.ByteOperations;

namespace SortingEngine;

internal class StreamsMergeExecutor
{
   private readonly IConfig _config;
   private string[] _files = null!;

   private LineMemory[] _outputBuffer = null!;
   private int _lastLine;
   private Memory<byte> _inputBuffer;
   public event EventHandler<SortingCompletedEventArgs>? OutputBufferFull;

   public StreamsMergeExecutor(IConfig config)
   {
      //todo static vs Guard
      _config = config ?? throw new ArgumentNullException(nameof(config));
   }

   public async Task<Result> MergeWithOrder()
   {
      _files = Directory.GetFiles(_config.TemporaryFolder);

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
         managers[i] = new DataChunkManager(_files[i], _inputBuffer[from..to], _config.Encoding,
            _config.RecordsBufferSize);
      }

      IComparer<LineMemory> comparer = new InSiteRecordsComparer(_inputBuffer);
      IndexPriorityQueue<LineMemory, IComparer<LineMemory>> queue =
         new IndexPriorityQueue<LineMemory, IComparer<LineMemory>>(_files.Length, comparer);


      for (int i = 0; i < _files.Length; i++)
      {
         (bool hasLine, LineMemory line) = await managers[i].TryGetNextLineAsync();
         if (hasLine)
         {
            queue.Enqueue(line, i);
            //todo remove
            if (line.Number == 8446805350952162698 || line.Number == 1243027978022674890)
            {
               var text = ByteToStringConverter.Convert(_inputBuffer[line.From..line.To]);
            }
         }
      }

      while (queue.Any())
      {
         var (line, streamIndex) = queue.Dequeue();
         var (lineAvailable, nextLine) = await managers[streamIndex].TryGetNextLineAsync();
         if(lineAvailable)
            queue.Enqueue(nextLine, streamIndex);

         _outputBuffer[_lastLine++] = line;
         if (_lastLine >= _outputBuffer.Length)
         {
            OnOutputBufferFull(new SortingCompletedEventArgs(_outputBuffer, _inputBuffer));
            _lastLine = 0;
         }
      }

      OnOutputBufferFull(new SortingCompletedEventArgs(_outputBuffer[.._lastLine], _inputBuffer));
      //todo flush output
      return Result.Ok;
   }

   private void CreateBuffers()
   {
      _inputBuffer = new byte[_config.MergeBufferSize * _files.Length].AsMemory();
      _outputBuffer = new LineMemory[_config.OutputBufferSize];
   }

   protected virtual void OnOutputBufferFull(SortingCompletedEventArgs e)
   {
      OutputBufferFull?.Invoke(this, e);
   }
}