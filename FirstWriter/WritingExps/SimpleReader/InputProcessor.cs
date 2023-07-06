using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleReader
{
   internal class InputProcessor
   {
      private readonly byte[] _eol;

      public InputProcessor(byte[] eol)
      {
         _eol = eol;
      }

      public void SplitOnLines(Span<byte> buffer, int[] linesPositions)
      {
         //compare with 
         // foreach (byte b in buffer)
         // {
         //    
         // }
         int lineIndex = 0;
         for (int i = 0; i < buffer.Length - 1; i++)
         {
            if (buffer[i] == _eol[0] && buffer[i + 1] == _eol[1])
            {
               linesPositions[lineIndex++] = i;
               i++;
            }
         }
      }
   }
}
