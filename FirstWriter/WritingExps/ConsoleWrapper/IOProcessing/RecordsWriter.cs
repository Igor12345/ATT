using System.Buffers;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using SortingEngine;
using SortingEngine.Entities;

namespace ConsoleWrapper.IOProcessing;

public class RecordsWriter : IAsyncDisposable, IDisposable
{
   private readonly string _filePath;
   private FileStream? _fileStream;
   private FileStream? _syncFileStream;

   private RecordsWriter(string filePath)
   {
      _filePath = Guard.NotNullOrEmpty(filePath);
   }

   public static RecordsWriter Create(string filePath)
   {
      CheckFilePath(filePath);
      RecordsWriter instance = new RecordsWriter(filePath);
      return instance;
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
      //todo dirty hack to support many encodings (*4)
      IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(Constants.MaxLineLength_UTF8 * 4);
      try
      {
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer.Memory.Span);
            source.Span[lines[i].From..lines[i].To].CopyTo(buffer.Memory.Span[length..]);
            await _fileStream.WriteAsync(buffer.Memory[..lines[i].To], token).ConfigureAwait(false);
         }

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
      _syncFileStream ??= new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None,
         bufferSize: 4096, false);

      try
      {
         //todo dirty hack to support many encodings (*4)
         //todo check for required size to prevent stackoverflow
         Span<byte> buffer = stackalloc byte[Constants.MaxLineLength_UTF8 * 4];
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer);
            source.Span[lines[i].From..lines[i].To].CopyTo(buffer[length..]);
            _syncFileStream.Write(buffer[..lines[i].To]);
         }

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