using System.Buffers;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

public class RecordsWriter : ILinesWriter, IAsyncDisposable
{
   private readonly int _charLength;
   private readonly ILogger _logger;
   private readonly string _filePath;
   private FileStream? _fileStream;
   private FileStream? _syncFileStream;

   private RecordsWriter(string filePath, int charLength, ILogger logger)
   {
      _charLength = Guard.Positive(charLength);
      _filePath = Guard.NotNullOrEmpty(filePath);
      _logger = Guard.NotNull(logger);
   }

   //todo delete b
   public static RecordsWriter Create(string filePath, int charLength, ILogger logger)
   {
      CheckFilePath(filePath);
      RecordsWriter instance = new RecordsWriter(filePath, charLength, logger);
      return instance;
   }

   //todo
   // [Conditional("Verbose")]
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }

   private static void CheckFilePath(string filePath)
   {
      var directory = Path.GetDirectoryName(filePath);
      if (!Path.Exists(directory))
         throw new ArgumentException("Wrong file path");
   }

   public async Task<Result> WriteRecordsAsync(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      _fileStream ??= File.Open(_filePath, FileMode.Create, FileAccess.Write);
      IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(Constants.MaxLineLengthUtf8 * _charLength);
      try
      {
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer.Memory.Span);
            source.Span[lines[i].From..lines[i].To].CopyTo(buffer.Memory.Span[length..]);
            await _fileStream.WriteAsync(buffer.Memory[..lines[i].To], token);
         }
         await _fileStream.FlushAsync(token);
         
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

   public Result WriteRecords(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      _syncFileStream ??= new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None,
         bufferSize: 1, false);

      // Console.WriteLine(
      //    $"Enter buffer file {_syncFileStream.Name}, position: {_syncFileStream.Position}, length: {_syncFileStream.Length}");
      
      byte[]? rented = null;
      try
      {
         int requiredLength = Constants.MaxLineLengthUtf8 * _charLength;
         Span<byte> buffer = requiredLength <= Constants.MaxStackLimit
            ? stackalloc byte[requiredLength]
            : rented = ArrayPool<byte>.Shared.Rent(requiredLength);
         
         buffer.Clear();
         
         //todo increase output buffer size (benchmark!)
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer);

            source.Span[lines[i].From..lines[i].To].CopyTo(buffer[length..]);
            int fullLength = length + lines[i].To - lines[i].From;
            _syncFileStream.Write(buffer[..fullLength]);
         }
         // _syncFileStream.Flush();
         
         // Console.WriteLine($"----> Saved {linesNumber} lines, from {initPosition} to {_syncFileStream.Position}");
         if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);
         
         // Console.WriteLine(
         //    $"Next buffer file {_syncFileStream.Name}, position: {_syncFileStream.Position}, length: {_syncFileStream.Length}");
        
         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
   }

   public async ValueTask DisposeAsync()
   {
      if (_syncFileStream != null) await _syncFileStream.DisposeAsync();
   }

   public void Dispose()
   {
      _syncFileStream?.Dispose();
   }
}