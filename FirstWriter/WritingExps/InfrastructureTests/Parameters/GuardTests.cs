using Guard = Infrastructure.Parameters.Guard;

namespace InfrastructureTests.Parameters
{
   public class GuardTests
   {
      [Fact]
      public void NotNullShouldGuard()
      {
         B b = new B();
         var c = Guard.NotNull(b);

         Assert.Same(b, c);

         Assert.Throws<ArgumentNullException>(() => Guard.NotNull<B>(null));
      }

      [Fact]
      public void PositiveShouldGuard()
      {
         int v = Guard.Positive(42);
         Assert.Equal(42, v);

         Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(-3));
      }

      private class B
      {
      }
   }
}
