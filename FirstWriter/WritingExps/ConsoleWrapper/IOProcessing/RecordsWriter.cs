using Infrastructure.ByteOperations;
using SortingEngine;
using SortingEngine.Entities;

namespace ConsoleWrapper.IOProcessing;

public class RecordsWriter : IAsyncDisposable
{
   private FileStream _fileStream = null!;

   private RecordsWriter()
   {
   }

   public static RecordsWriter Create(string fullFileName)
   {
      RecordsWriter instance = new RecordsWriter
      {
         _fileStream = File.Open(fullFileName, FileMode.Create, FileAccess.Write)
      };
      return instance;
   }

   public async Task<Result> WriteRecords(LineMemory[] records, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      try
      {
         await using LongToBytesConverter longToBytes = new LongToBytesConverter();

         for (int i = 0; i < records.Length; i++)
         {
            long position = _fileStream.Position;
            var (numberBytes, length) = longToBytes.ConvertLongToBytes(records[i].Number);
            await _fileStream.WriteAsync(numberBytes[..length], token).ConfigureAwait(false);
            if (position + length != _fileStream.Position)
               throw new InvalidOperationException("Wrong position");
            position += length;

            await _fileStream.WriteAsync(source[records[i].From..records[i].To], token).ConfigureAwait(false);
            if (position - records[i].From + records[i].To != _fileStream.Position)
               throw new InvalidOperationException("Wrong position");
         }

         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
   }

   public ValueTask DisposeAsync()
   {
      return _fileStream.DisposeAsync();
   }
}