using System.Buffers;
using Infrastructure.Parameters;

namespace SortingEngine.DataStructures
{
   //todo test
   public class ExpandingStorage<T> : IDisposable
   {
      private readonly int _chunkSize;
      //todo either use lock or remove volatile
      private volatile int _lastBuffer = -1;
      private long _currentIndex;
      private readonly List<T[]> _buffers;

      public ExpandingStorage(int chunkSize)
      {
         _chunkSize = Guard.Positive(chunkSize);
         _buffers = new List<T[]>();
      }

      private void RentSpace()
      {
         T[] array = ArrayPool<T>.Shared.Rent(_chunkSize);
         
         Interlocked.Increment(ref _lastBuffer);
         _buffers.Add(array);
         _currentIndex = 0;
      }

      public T this[long i]
      {
         get
         {
            //todo possible error, but I'm not expect it in real cases
            int buffer = (int)(i / _chunkSize);
            int position = (int)(i % _chunkSize);
            return _buffers[buffer][position];
         }
      }

      public long CurrentCapacity => _lastBuffer * _chunkSize;

      public void CopyTo(T[] destination, int length)
      {
         for (int i = 0; i <= _lastBuffer; i++)
         {
            int from = i * _chunkSize;
            int right = (i + 1) * _chunkSize;
            int to = Math.Min(right, length);
            _buffers[i].AsSpan(..to).CopyTo(destination.AsSpan(from..to));
            if (right >= length)
               break;
         }
      }

      public void Add(T item)
      {
         if (_lastBuffer < 0 || _currentIndex >= _chunkSize)
            RentSpace();
         _buffers[_lastBuffer][_currentIndex++] = item;
      }

      public void Clear()
      {
         Dispose();
      }

      public void Dispose()
      {
         foreach (var buffer in _buffers)
         {
            ArrayPool<T>.Shared.Return(buffer);
         }

         _lastBuffer = -1;
         _currentIndex = 0;
      }
   }
}
