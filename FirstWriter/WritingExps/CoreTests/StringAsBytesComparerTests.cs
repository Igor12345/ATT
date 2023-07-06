using SimpleReader;

namespace CoreTests
{
   public class StringAsBytesComparerTests
   {
      [Theory]
      [InlineData("", "")]
      [InlineData("b", "")]
      [InlineData("", "c")]
      [InlineData("a b", "a b")]
      [InlineData("aB", "ab")]
      [InlineData("_a", "_a")]
      [InlineData("abc", "ab")]
      [InlineData("ab", "abc")]
      [InlineData("abcdefg", "abcdefg")]
      [InlineData("abcdefg", "abc2defg")]
      public void ShouldCorrectlyCompareStrings(string left, string right)
      {
         Span<byte> leftSpan = new byte[left.Length];
         Span<byte> rightSpan = new byte[right.Length];

         for (int i = 0; i < left.Length; i++)
         {
            leftSpan[i] = (byte)left[i];
         }
         for (int i = 0; i < right.Length; i++)
         {
            rightSpan[i] = (byte)right[i];
         }

         int stringComparison = ConvertToOne(string.CompareOrdinal(left, right));
         int bytesComparison = ConvertToOne(StringAsBytesComparer.Compare(leftSpan, rightSpan));
         Assert.Equal(stringComparison, bytesComparison);
      }

      private int ConvertToOne(int value)
      {
         return value switch
         {
            > 0 => 1,
            < 0 => -1,
            _ => 0
         };
      }
   }
}