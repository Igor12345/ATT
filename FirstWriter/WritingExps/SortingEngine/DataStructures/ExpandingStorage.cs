using System.Buffers;
using Infrastructure.Parameters;

namespace SortingEngine.DataStructures
{
   //todo test
   public class ExpandingStorage<T> : IDisposable
   {
      private readonly int _chunkSize;
      //todo either use lock or remove volatile
      private volatile int _lastBuffer;
      private long _currentIndex;
      private readonly Dictionary<int, LinesChunk<T>> _buffers;

      public ExpandingStorage(int chunkSize)
      {
         _chunkSize = Guard.Positive(chunkSize);
         _buffers = new Dictionary<int, LinesChunk<T>>();
      }

      private LinesChunk<T> RentSpace()
      {
         T[] array = ArrayPool<T>.Shared.Rent(_chunkSize);
         int index = Interlocked.Increment(ref _lastBuffer);
         var chunk = new LinesChunk<T>(index, array);
         _buffers.Add(index, chunk);
         _currentIndex = 0;
         return chunk;
      }

      public T this[long i]
      {
         get
         {
            //todo possible error, but I'm not expect it in real cases
            int buffer = (int)(i / _chunkSize);
            int position = (int)(i%_chunkSize);
            return _buffers[buffer].Buffer[position];
         }
      }

      public long CurrentCapacity => _lastBuffer * _chunkSize;

      public void CopyTo(T[] destination, int length)
      {
         for (int i = 0; i < _lastBuffer; i++)
         {
            int from = i * _chunkSize;
            int right = (i + 1) * _chunkSize;
            int to = Math.Min(right, length);
            _buffers[i].Buffer.CopyTo(destination.AsSpan(from..to));
            if (right >= length)
               break;
         }
      }

      public void Add(T item)
      {
         if (_currentIndex >= _chunkSize)
            RentSpace();
         _buffers[_lastBuffer].Buffer[_currentIndex++] = item;
      }

      public void Clear()
      {
         Dispose();
      }

      public void Dispose()
      {
         foreach (var linesChunk in _buffers)
         {
            ArrayPool<T>.Shared.Return(linesChunk.Value.Buffer);
         }

         _lastBuffer = 0;
         _currentIndex = 0;
      }
   }

   public record struct LinesChunk<T>(int Index, T[] Buffer);
}
