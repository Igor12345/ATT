﻿using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.Sorters;
using Infrastructure.Parameters;
using SortingEngine.RowData;

namespace SortingEngine;

public sealed class StreamsMergeExecutor
{
   private readonly IConfig _config;
   private readonly ILinesWriter _linesWriter;
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
   public StreamsMergeExecutor(IConfig config, ILinesWriter linesWriter)
   {
      //todo static vs Guard
      _config = config ?? throw new ArgumentNullException(nameof(config));
      _linesWriter = Guard.NotNull(linesWriter);
   }

   //todo file system dependence
   public async Task<Result> MergeWithOrder()
   {
      _files = Directory.GetFiles(_config.TemporaryFolder);

      CreateBuffers();

      return await ExecuteMerge();
   }

   private void CreateBuffers()
   {
      _inputBuffer = new byte[_config.MergeBufferLength * _files.Length].AsMemory();
      _outputBuffer = new LineMemory[_config.OutputBufferLength];
   }

   private async Task<Result> ExecuteMerge()
   {
      DataChunkManager[] managers = new DataChunkManager[_files.Length];
      for (int i = 0; i < _files.Length; i++)
      {
         //todo configure LineMemory creation
         int from = i * _config.MergeBufferLength;
         int to = (i + 1) * _config.MergeBufferLength;
         managers[i] = new DataChunkManager(_files[i], _inputBuffer[from..to], _config.Encoding,
            _config.RecordsBufferLength);
      }

      IComparer<LineMemory> comparer = new OnSiteLinesComparer(_inputBuffer);
      IndexPriorityQueue<LineMemory, IComparer<LineMemory>> queue =
         new IndexPriorityQueue<LineMemory, IComparer<LineMemory>>(_files.Length, comparer);

      for (int i = 0; i < _files.Length; i++)
      {
         (bool hasLine, LineMemory line) = await managers[i].TryGetNextLineAsync();
         if (hasLine)
         {
            queue.Enqueue(line, i);
         }
      }

      while (queue.Any())
      {
         var (line, streamIndex) = queue.Dequeue();
         var (lineAvailable, nextLine) = await managers[streamIndex].TryGetNextLineAsync();
         if (lineAvailable)
            queue.Enqueue(nextLine, streamIndex);

         _outputBuffer[_lastLine++] = line;
         if (_lastLine >= _outputBuffer.Length)
         {
            Result result = WriteLinesFromBuffer(_outputBuffer, _outputBuffer.Length, _inputBuffer);
            if (!result.Success)
               return result;
            // OnOutputBufferFull(new SortingCompletedEventArgs(_outputBuffer, _outputBuffer.Length, _inputBuffer));
            _lastLine = 0;
         }
      }

      // OnOutputBufferFull(new SortingCompletedEventArgs(_outputBuffer, _lastLine, _inputBuffer));
      //todo
      return _lastLine == 0 ? Result.Ok : WriteLinesFromBuffer(_outputBuffer, _lastLine, _inputBuffer);
   }

   private Result WriteLinesFromBuffer(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      return _linesWriter.WriteRecords(lines, linesNumber, source);
   }

   private void OnOutputBufferFull(SortingCompletedEventArgs e)
   {
      OutputBufferFull?.Invoke(this, e);
   }
}