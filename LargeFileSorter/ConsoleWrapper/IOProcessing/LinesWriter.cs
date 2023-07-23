using System.Buffers;
using System.Text;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;
using Constants = Infrastructure.ByteOperations.Constants;

namespace ConsoleWrapper.IOProcessing;

//not the best decision, only as a temporary case, for this proof of concept
public class LinesWriter : IOneTimeLinesWriter, ISeveralTimesLinesWriter
{
   private readonly Encoding _encoding;

   private readonly int _bufferSize;
   // private readonly int _charLength;
   private readonly int _maxLineLength;
   private readonly string? _filePath;
   private FileStream? _fileStream;
   private FileStream? _syncFileStream;

   private LinesWriter(string filePath, int charLength, int bufferSize, Encoding encoding) : this(charLength,
      bufferSize, encoding)
   {
      _filePath = Guard.NotNullOrEmpty(filePath);
   }

   private LinesWriter(int maxLineLength, int bufferSize, Encoding encoding)
   {
      _maxLineLength = Guard.Positive(maxLineLength);
      _bufferSize = Guard.Positive(bufferSize);
      _encoding = Guard.NotNull(encoding);
   }

   public static IOneTimeLinesWriter CreateForOnceWriting(int maxLineLength, int bufferSize,
      Encoding encoding)
   {
      LinesWriter instance = new LinesWriter(maxLineLength, bufferSize, encoding);
      return instance;
   }

   public static ISeveralTimesLinesWriter CreateForMultipleWriting(string filePath, int maxLineLength, int bufferSize,
      Encoding encoding)
   {
      LinesWriter instance = new LinesWriter(filePath, maxLineLength, bufferSize, encoding);
      return instance;
   }

   private async Task<Result> WriteLinesInternalAsync(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      _fileStream ??= new FileStream(_filePath!, FileMode.Create, FileAccess.Write, FileShare.None,
         bufferSize: _bufferSize, true);
      IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(_maxLineLength);
      try
      {
         for (int i = 0; i < linesNumber; i++)
         {
            int length = LongToBytesConverter.WriteULongToBytes(lines[i].Number, buffer.Memory.Span);
            source.Span[lines[i].From..lines[i].To].CopyTo(buffer.Memory.Span[length..]);
            int fullLength = length + lines[i].To - lines[i].From;
            await _fileStream.WriteAsync(buffer.Memory[..fullLength], token);
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

   private Result WriteLinesInternal(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      _syncFileStream ??= new FileStream(_filePath!, FileMode.Create, FileAccess.Write, FileShare.None,
         bufferSize: _bufferSize, false);
      
      byte[]? rented = null;
      try
      {
         Span<byte> buffer = _maxLineLength <= Constants.MaxStackLimit
            ? stackalloc byte[_maxLineLength]
            : rented = ArrayPool<byte>.Shared.Rent(_maxLineLength);
         
         //todo increase output buffer size? (benchmark!)
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

   Result IOneTimeLinesWriter.WriteLines(string filePath, Line[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      try
      {
         _syncFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: _bufferSize, false);
         return WriteLinesInternal(lines, linesNumber, source);
      }
      finally
      {
         _syncFileStream?.Dispose();
         _syncFileStream = null;
      }
   }

   async Task<Result> IOneTimeLinesWriter.WriteLinesAsync(string filePath, Line[] lines, int linesNumber, ReadOnlyMemory<byte> source,
      CancellationToken token)
   {
      try
      {
         _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: _bufferSize, true);
         return await WriteLinesInternalAsync(lines, linesNumber, source, token);
      }
      finally
      {
         if (_fileStream != null) await _fileStream.DisposeAsync();
         _fileStream = null;
      }
   }

   Result ISeveralTimesLinesWriter.WriteLines(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source)
   {
      return WriteLinesInternal(lines, linesNumber, source);
   }

   Task<Result> ISeveralTimesLinesWriter.WriteLinesAsync(Line[] lines, int linesNumber, ReadOnlyMemory<byte> source, CancellationToken token)
   {
      return WriteLinesInternalAsync(lines, linesNumber, source, token);
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