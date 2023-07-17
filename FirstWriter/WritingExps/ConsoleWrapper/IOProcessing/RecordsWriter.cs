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
   private FileStream? _onlyNumbersStream;
   private StreamWriter? _numbersWriter;

   private RecordsWriter(string filePath, int charLength, ILogger logger, bool writeNumbers)
   {
      _writeNumbers = false;//writeNumbers;
      _charLength = Guard.Positive(charLength);
      _filePath = Guard.NotNullOrEmpty(filePath);
      _logger = Guard.NotNull(logger);
   }

   //todo delete b
   public static RecordsWriter Create(string filePath, int charLength, ILogger logger, bool b = false)
   {
      CheckFilePath(filePath);
      RecordsWriter instance = new RecordsWriter(filePath, charLength, logger, b);
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

      if (_writeNumbers)
         _numbersWriter ??= new StreamWriter(_filePath + "numbers");

      Console.WriteLine(
         $"Enter buffer file {_syncFileStream.Name}, position: {_syncFileStream.Position}, length: {_syncFileStream.Length}");

      
      byte[]? rented = null;
      try
      {
         //todo dirty hack to support many encodings (*4)
         //todo check for required size to prevent stackoverflow
         int requiredLength = Constants.MaxLineLengthUtf8 * _charLength;
         Span<byte> buffer = requiredLength <= Constants.MaxStackLimit
            ? stackalloc byte[requiredLength]
            : rented = ArrayPool<byte>.Shared.Rent(requiredLength);
         
         buffer.Clear();
         
         //todo increase output buffer size (benchmark!)
         for (int i = 0; i < linesNumber; i++)
         {
            //todo
            if (lines[i].Number == 4307412542716114199 )
            {
               LineMemory next = default;
               LineMemory prev = default;
               if (i+1 < linesNumber)
               {
                  next = lines[i + 1];
               }

               if (i > 0)
               {
                  prev = lines[i - 1];
               }

               Console.WriteLine(
                  $"-------->>>> Line 4307412542716114199 at i={i} in buffer {bufNum++}, next: {next.Number}, prev: {prev.Number}");
            }
            
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer);

            if (_writeNumbers)
            {
               _syncFileStream.Write(buffer[..length]);
               _numbersWriter.WriteLine(lines[i].Number);
            }
            else
            {
               source.Span[lines[i].From..lines[i].To].CopyTo(buffer[length..]);
               int fullLength = length + lines[i].To - lines[i].From;
               _syncFileStream.Write(buffer[..fullLength]);
            }
         }
         // _syncFileStream.Flush();
         
         // Console.WriteLine($"----> Saved {linesNumber} lines, from {initPosition} to {_syncFileStream.Position}");
         if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);
         
         Console.WriteLine(
            $"Next buffer file {_syncFileStream.Name}, position: {_syncFileStream.Position}, length: {_syncFileStream.Length}");

         
         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
   }
   
   //todo del
   private int bufNum = 0;
   private bool _writeNumbers;

   public async ValueTask DisposeAsync()
   {
      if (_syncFileStream != null) await _syncFileStream.DisposeAsync();
   }

   public void Dispose()
   {
      if (_syncFileStream != null)
         Console.WriteLine(
            $"Dispose file {_syncFileStream.Name}, position: {_syncFileStream.Position}, is async: {_syncFileStream.IsAsync}, length: {_syncFileStream.Length}");
      _syncFileStream?.Dispose();
      _numbersWriter?.Dispose();
   }
}