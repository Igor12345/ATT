using Infrastructure.Parameters;

namespace SortingEngine.Sorting
{
   //can be internal if make this assembly friendly for tests
   //There is an "official" implementation: System.Collections.Generic.PriorityQueue
   //but in this case it was easier to use custom
   //Algorithms (4th Edition) by Robert Sedgewick, Kevin Wayne
   public class IndexPriorityQueue<T, TC> where TC : IComparer<T>
   {
      private readonly TC _comparer;

      private readonly int _capacity;
      
      private readonly (T, int)[] _items;
      private int _currentSize = -1;

      public IndexPriorityQueue(int capacity, TC comparer)
      {
         _capacity = Guard.Positive(capacity);
         _items = new (T, int)[Capacity];
         _comparer = Guard.NotNull(comparer);
      }

      public IndexPriorityQueue((T, int)[] emptyBuffer, TC comparer)
      {
         var _ = Guard.Positive(emptyBuffer.Length);
         _items = emptyBuffer;
         _comparer = Guard.NotNull(comparer);
      }

      public int Capacity => _capacity;

      public void Enqueue(T item, int key)
      {
         _items[++_currentSize] = (item, key);
         Swim(_currentSize);
      }

      public void Enqueue((T item, int key) value)
      {
         Enqueue(value.item, value.key);
      }

      public (T, int) Dequeue()
      {
         if (!Any())
            throw new InvalidOperationException("There are no items in the queue.");
         var top = _items[0];
         Exchange(0, _currentSize);
         _items[_currentSize--] = default;
         Sink(0);
         return top;
      }

      public (T, int) Peek()
      {
         if (!Any())
            throw new InvalidOperationException("There are no items in the queue.");
         return _items[0];
      }

      public bool Any()
      {
         return _currentSize >= 0;
      }

      private void Exchange(int left, int right)
      {
         (_items[left], _items[right]) = (_items[right], _items[left]);
      }

      private bool More(int i, int j)
      {
         return _comparer.Compare(_items[i].Item1, _items[j].Item1) > 0;
      }

      private void Swim(int k)
      {
         while (k > 0 && More(k / 2, k))
         {
            Exchange(k / 2, k);
            k = k / 2;
         }
      }

      private void Sink(int k)
      {
         while (2 * k <= _currentSize)
         {
            int j = 2 * k;
            if (j < _currentSize && More(j, j + 1))
               j++;
            if (!More(k, j))
               break;
            Exchange(k, j);
            k = j;
         }
      }
   }
}
