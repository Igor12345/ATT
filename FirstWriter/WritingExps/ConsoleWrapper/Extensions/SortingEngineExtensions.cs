using SortingEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleWrapper.IOProcessing;

namespace ConsoleWrapper.Extensions
{
   internal static class SortingEngineExtensions
   {
      public static async Task<int> SortFile(this RecordsSetSorter sorter, string fileName, Encoding encoding, CancellationToken cancellationToken)
      {
         IBytesProducer bytesProducer = CreateBytesReader(fileName, encoding);
         var result = await sorter.SortAsync(bytesProducer, cancellationToken);
         return result.Success ? 0 : -1;
      }

      //todo rename
      private static IBytesProducer CreateBytesReader(string fileName, Encoding encoding)
      {
         return new LongFileReader(fileName, encoding, null, CancellationToken.None);
      }
   }
}
