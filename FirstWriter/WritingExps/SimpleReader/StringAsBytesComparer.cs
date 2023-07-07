namespace SimpleReader;

public class StringAsBytesComparer
{
   
   public static int Compare(ReadOnlySpan<byte> strA, ReadOnlySpan<byte> strB)
   {
      //todo
      if (strA == null)
      {
         return -1;
      }

      if (strB == null)
      {
         return 1;
      }

      if (strA.Length == 0 && strB.Length == 0)
         return 0;

      if (strA.Length == 0)
         return -1;

      if (strB.Length == 0)
         return 1;

      // Most common case, first character is different.
      if ((strA[0] - strB[0]) != 0)
      {
         return strA[0] - strB[0];
      }

      int length = Math.Min(strA.Length, strB.Length);
      int diffOffset = -1;

      int i = -1;
      // unroll the loop
      while (length >= 5)
      {
         if (strA[++i] != strB[i])
         {
            diffOffset = i;
            break;
         }

         if (strA[++i] != strB[i])
         {
            diffOffset = i;
            break;
         }

         if (strA[++i] != strB[i])
         {
            diffOffset = i;
            break;
         }

         if (strA[++i] != strB[i])
         {
            diffOffset = i;
            break;
         }

         if (strA[++i] != strB[i])
         {
            diffOffset = i;
            break;
         }

         length -= 5;
      }

      if (diffOffset != -1)
      {
         // we already see a difference in the unrolled loop above
         return strA[diffOffset] - strB[diffOffset];
      }

      // now go back to slower code path and do comparison on 4 bytes one time.
      // Following code also take advantage of the fact strings will 
      // use even numbers of characters (runtime will have a extra zero at the end.)
      // so even if length is 1 here, we can still do the comparsion.  
      while (length > 0)
      {
         int c;

         length--;
         if ((c = strA[length] - strB[length]) != 0)
         {
            return c;
         }
      }

      // At this point, we have compared all the characters in at least one string.
      // The longer string will be larger.
      return strA.Length - strB.Length;
   }
}