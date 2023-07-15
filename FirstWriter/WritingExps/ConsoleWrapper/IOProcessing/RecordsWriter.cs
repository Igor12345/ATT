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
      //todo in not necessary for the last file
      RecordsWriter instance = new RecordsWriter
      {
         _fileStream = File.Open(fullFileName, FileMode.Create, FileAccess.Write)
      };
      return instance;
   }

   public async Task<Result> WriteRecordsAsync(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      try
      {
         await using LongToBytesConverter longToBytes = new LongToBytesConverter();

         for (int i = 0; i < linesNumber; i++)
         {
            //
            long position = _fileStream.Position;
            var (numberBytes, length) = longToBytes.ConvertLongToBytes(lines[i].Number);
            await _fileStream.WriteAsync(numberBytes[..length], token).ConfigureAwait(false);
            if (position + length != _fileStream.Position)
               throw new InvalidOperationException("Wrong position");
            position += length;

            await _fileStream.WriteAsync(source[lines[i].From..lines[i].To], token).ConfigureAwait(false);
            if (position - lines[i].From + lines[i].To != _fileStream.Position)
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