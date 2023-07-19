using System.Buffers;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

//not the best decision, only as a temporary case, for this proof of concept
public class LinesWriter : IOneTimeLinesWriter, ISeveralTimesLinesWriter
{
   private readonly int _bufferSize;
   private readonly int _charLength;
   private readonly ILogger _logger;
   private readonly string _filePath;
   private FileStream? _fileStream;
   private FileStream? _syncFileStream;

   private LinesWriter(string filePath, int charLength, int bufferSize, ILogger logger) : this(charLength, bufferSize,
      logger)
   {
      _filePath = Guard.NotNullOrEmpty(filePath);
   }

   private LinesWriter(int charLength, int bufferSize, ILogger logger)
   {
      _charLength = Guard.Positive(charLength);
      _bufferSize = Guard.Positive(bufferSize);
      _logger = Guard.NotNull(logger);
   }

   public static IOneTimeLinesWriter CreateForOnceWriting(int charLength, int bufferSize, ILogger logger)
   {
      LinesWriter instance = new LinesWriter(charLength, bufferSize, logger);
      return instance;
   }

   public static ISeveralTimesLinesWriter CreateForMultipleWriting(string filePath, int charLength, int bufferSize, ILogger logger)
   {
      LinesWriter instance = new LinesWriter(filePath, charLength, bufferSize, logger);
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

   private async Task<Result> WriteRecordsAsync(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      _fileStream ??= new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None,
         bufferSize: _bufferSize, true);
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

   private Result WriteRecords(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      _syncFileStream ??= new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None,
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
            _syncFileStream.Write(buffer[..fullLength]);
         }
         _syncFileStream.Flush();
         if (rented != null)
            ArrayPool<byte>.Shared.Return(rented);
         
         return Result.Ok;
      }
      catch (Exception e)
      {
         return Result.Error(e.Message);
      }
   }

   Result IOneTimeLinesWriter.WriteRecords(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      try
      {
         _syncFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: _bufferSize, false);
         return WriteRecords(lines, linesNumber, source);
      }
      finally
      {
         _syncFileStream?.Dispose();
         _syncFileStream = null;
      }
   }

   async Task<Result> IOneTimeLinesWriter.WriteRecordsAsync(string filePath, LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      try
      {
         _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: _bufferSize, true);
         return await WriteRecordsAsync(lines, linesNumber, source, token);
      }
      finally
      {
         if (_fileStream != null) await _fileStream.DisposeAsync();
         _fileStream = null;
      }
   }

   Result ISeveralTimesLinesWriter.WriteRecords(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      return WriteRecords(lines, linesNumber, source);
   }

   Task<Result> ISeveralTimesLinesWriter.WriteRecordsAsync(LineMemory[] lines, int linesNumber, ReadOnlyMemory<byte> source, CancellationToken token)
   {
      return WriteRecordsAsync(lines, linesNumber, source, token);
   }

   public async ValueTask DisposeAsync()
   {
      if (_fileStream != null) await _fileStream.DisposeAsync();
      if (_syncFileStream != null)
         throw new InvalidOperationException("Erroneous class usage LinesWriter.");
   }

   public void Dispose()
   {
      _syncFileStream?.Dispose();
      if (_fileStream != null)
         throw new InvalidOperationException("Erroneous class usage LinesWriter.");
   }
}