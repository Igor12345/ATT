

using System.Text;
using SortingEngine.Comparators;

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
      [InlineData("abcdef", "abcdefg")]
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

      //todo
      // https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction
      [Fact]
      public void TryCyrillic()
      {
         Encoding encoding = Encoding.UTF8;
         Encoding encoding32 = Encoding.UTF32;
         string one = "abcd";
         string two = "Ππ Σσ";
         // string two = "абвг \u03a0\u03a3";

         var bytesOne = encoding.GetBytes(one);
         var bytesTwo = encoding.GetBytes(two);

         var strOne = string.Join(", ", bytesOne);
         var strTwo = string.Join(", ", bytesTwo);
         
         // Assert.Same(strOne, strTwo);

         var a = string.CompareOrdinal("a", "b");
         var A = string.CompareOrdinal("a", "A");
         var b = string.CompareOrdinal("b", "б");
         var c = string.CompareOrdinal("a", "π");
         var d = string.CompareOrdinal("Σ", "π");
         var d1 = string.CompareOrdinal("σ", "π");
         var d2 = string.CompareOrdinal("σ", "Σ");
         var t = d;
      }
   }
}