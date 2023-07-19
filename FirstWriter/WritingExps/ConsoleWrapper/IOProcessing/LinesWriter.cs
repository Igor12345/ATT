using System.Buffers;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

public sealed class LinesWriter : ILinesWriter
{
   private readonly int _bufferSize;
   private readonly int _charLength;
   private readonly ILogger _logger;

   public LinesWriter(int charLength, int bufferSize, ILogger logger)
   {
      _charLength = Guard.Positive(charLength);
      _bufferSize = Guard.Positive(bufferSize);
      _logger = Guard.NotNull(logger);
   }

   //todo
   // [Conditional("Verbose")]
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }

   public async Task<Result> WriteRecordsAsync(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
         bufferSize: _bufferSize, true);
      IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(Constants.MaxLineLengthUtf8 * _charLength);
      try
      {
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer.Memory.Span);
            source.Span[lines[i].From..lines[i].To].CopyTo(buffer.Memory.Span[length..]);
            await fileStream.WriteAsync(buffer.Memory[..lines[i].To], token);
         }
         await fileStream.FlushAsync(token);
         
         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
      finally
      {
         buffer.Dispose();
      }
   }

   public Result WriteRecords(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      using FileStream syncFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
         bufferSize: _bufferSize, false);
      
      byte[]? rented = null;
      try
      {
         int requiredLength = Constants.MaxLineLengthUtf8 * _charLength;
         Span<byte> buffer = requiredLength <= Constants.MaxStackLimit
            ? stackalloc byte[requiredLength]
            : rented = ArrayPool<byte>.Shared.Rent(requiredLength);
         
         //todo increase output buffer size (benchmark!)
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer);

            source.Span[lines[i].From..lines[i].To].CopyTo(buffer[length..]);
            int fullLength = length + lines[i].To - lines[i].From;
            syncFileStream.Write(buffer[..fullLength]);
         }
         syncFileStream.Flush();
         if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);
        
         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
   }
}