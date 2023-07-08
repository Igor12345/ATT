using InfoStructure;
using SortingEngine;
using SortingEngine.Entities;

namespace ConsoleWrapper.IOProcessing;

public class RecordsWriter
{
   public async Task<Result> WriteRecords(string fullFileName, LineMemory[] records, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      try
      {
         await using FileStream fileStream = File.Open(fullFileName, FileMode.Create, FileAccess.Write);
         await using LongToBytesConverter longToBytes = new LongToBytesConverter();

         for (int i = 0; i < records.Length; i++)
         {
            var (numberBytes, length) = longToBytes.ConvertLongToBytes(records[i].Number);
            await fileStream.WriteAsync(numberBytes[..length], token).ConfigureAwait(false);
            await fileStream.WriteAsync(source[records[i].From..records[i].To], token).ConfigureAwait(false);
         }

         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
   }
}