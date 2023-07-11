using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SortingEngine.Entities;

namespace SortingEngine.DataStructures
{
   internal class ExpandingStorage<T> : IDisposable
   {
      private int _chunkSize;
      private volatile int _lastIndex;
      private Dictionary<int, LinesChunk> _buffer;

      public ExpandingStorage()
      {
         _chunkSize = 100_000;
         _buffer = new Dictionary<int, LinesChunk>();
      }

      public LinesChunk RentSpace()
      {
         LineMemory[] array = ArrayPool<LineMemory>.Shared.Rent(_chunkSize);
         int index = Interlocked.Increment(ref _lastIndex);
         var chunk = new LinesChunk(index, array);
         _buffer.Add(index, chunk);
         return chunk;
      }

      public void Dispose()
      {
         foreach (var linesChunk in _buffer)
         {
            ArrayPool<LineMemory>.Shared.Return(linesChunk.Value.Buffer);
         }
      }

      public LinesChunk this[int i] => _buffer[i];
   }

   public record struct LinesChunk(int Index, LineMemory[] Buffer);
}
