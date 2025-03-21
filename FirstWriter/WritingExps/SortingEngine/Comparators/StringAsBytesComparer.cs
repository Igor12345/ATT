﻿namespace SortingEngine.Comparators;

public class StringAsBytesComparer
{
   //todo link to GitHub
   public static int Compare(ReadOnlySpan<byte> strA, ReadOnlySpan<byte> strB)
   {
      if (strA.Length == 0 && strB.Length == 0)
         return 0;

      if (strA.Length == 0)
         return -1;

      if (strB.Length == 0)
         return 1;

      // Most common case, first character is different.
      //all lines start from ". "
      if ((strA[2] - strB[2]) != 0)
      {
         return strA[2] - strB[2];
      }

      int length = Math.Min(strA.Length, strB.Length);
      int diffOffset = -1;

      int i = 0;
      // unroll the loop
      while (length > 5)
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
      
      while (length > 0)
      {
         int c;

         length--;
         if ((c = strA[length] - strB[length]) != 0)
         {
            return c;
         }
      }
      
      return strA.Length - strB.Length;
   }
}