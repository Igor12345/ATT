using SortingEngine.DataStructures;
using SortingEngine.Sorting;

namespace SortingEngineTests.Sorting
{
   public class IndexPriorityQueueTests
   {
      [Fact]
      public void QueueCanBeCreated()
      {
         IndexPriorityQueue<int, Comparer<int>> queue =
            new IndexPriorityQueue<int, Comparer<int>>(4, Comparer<int>.Default);

         Assert.NotNull(queue);
         Assert.Equal(4, queue.Capacity);
         Assert.False(queue.Any());
      }

      [Fact]
      public void ShouldNotCreateQueueWithNegativeCapacity()
      {
         Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IndexPriorityQueue<int, Comparer<int>>(-4, Comparer<int>.Default));
      }

      [Fact]
      public void ShouldBeAbleToAddValuesToQueue()
      {
         IndexPriorityQueue<int, IComparer<int>> queue = CreateSampleQueue(new[] { (23, 4) });

         Assert.True(queue.Any());
      }

      [Fact]
      public void ShouldBeAbleToPeekValuesFromQueueMultipleTimes()
      {
         IndexPriorityQueue<int, IComparer<int>> queue = CreateSampleQueue(new[] { (23, 4) });

         (int v, int k) = queue.Peek();
         Assert.Multiple(
            () => Assert.Equal(23, v),
            () => Assert.Equal(4, k));

         (int v1, int k1) = queue.Peek();
         Assert.Multiple(
            () => Assert.Equal(23, v1),
            () => Assert.Equal(4, k1));
      }

      [Fact]
      public void ShouldBeAbleDequeueFromQueueOnlyOnce()
      {
         IndexPriorityQueue<int, IComparer<int>> queue = CreateSampleQueue(new[] { (23, 4) });

         (int v, int k) = queue.Dequeue();
         Assert.Multiple(
            () => Assert.Equal(23, v),
            () => Assert.Equal(4, k));

         Assert.False(queue.Any());
      }

      [Fact]
      public void TheQueueShouldOrderValues()
      {
         IndexPriorityQueue<int, Comparer<int>> queue =
            new IndexPriorityQueue<int, Comparer<int>>(4, Comparer<int>.Default);

         int value1 = 23;
         int value2 = 34;
         int key1 = 2;
         int key2 = 16;
         queue.Enqueue(value1, key1);
         queue.Enqueue(value2, key2);

         (int v, int k) = queue.Peek();
         Assert.Equal(value2, v);
         Assert.Equal(key2, k);

         (v, k) = queue.Dequeue();
         Assert.Equal(value2, v);
         Assert.Equal(key2, k);

         Assert.True(queue.Any());

         (v, k) = queue.Dequeue();
         Assert.Equal(value1, v);
         Assert.Equal(key1, k);

         Assert.False(queue.Any());
      }

      [Fact]
      public void TheQueueShouldUseProvidedComparer()
      {
         IComparer<int> reverseComparer = new ReverseComparer();
         (int, int)[] input =
         {
            (4, 2),
            (5, 3),
            (1, 4),
            (2, 3)
         };
         IndexPriorityQueue<int, IComparer<int>> queue = CreateSampleQueue(input, reverseComparer);

         List<int> sortedValues = new List<int>();
         while (queue.Any())
         {
            sortedValues.Add(queue.Dequeue().Item1);
         }

         Assert.Equal(4, sortedValues.Count);
         Assert.Collection(sortedValues,
            item => Assert.Equal(1, item),
            item => Assert.Equal(2, item),
            item => Assert.Equal(4, item),
            item => Assert.Equal(5, item)
         );
      }

      private static IndexPriorityQueue<T, IComparer<T>> CreateSampleQueue<T>((T, int)[] items,
         IComparer<T>? comparer = null)
      {
         comparer ??= Comparer<T>.Default;
         IndexPriorityQueue<T, IComparer<T>> queue =
            new IndexPriorityQueue<T, IComparer<T>>(4, comparer);

         foreach ((T, int) item in items)
         {
            queue.Enqueue(item);
         }

         return queue;
      }

      private class ReverseComparer : IComparer<int>
      {
         public int Compare(int x, int y)
         {
            return -1 * Comparer<int>.Default.Compare(x, y);
         }
      }
   }
}
