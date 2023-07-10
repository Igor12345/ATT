using SortingEngine.Sorters;

namespace CoreTests.Math
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

      //It makes no sense to split this test into parts under the current circumstances. It is too simple.
      [Fact]
      public void ShouldBeAbleAddValuesToQueue()
      {
         IndexPriorityQueue<int, Comparer<int>> queue =
            new IndexPriorityQueue<int, Comparer<int>>(4, Comparer<int>.Default);

         int value = 23;
         int key = 2;
         queue.Enqueue(value,key);

         Assert.True(queue.Any());

         var (v,k) = queue.Peek();
         Assert.Equal(value,v);
         Assert.Equal(key,k);

         (v, k) = queue.Dequeue();
         Assert.Equal(value, v);
         Assert.Equal(key, k);
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

         var (v, k) = queue.Peek();
         Assert.Equal(value2, v);
         Assert.Equal(key2, k);

         (v, k) = queue.Peek();
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
   }
}
