using System.Text;
using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

//This class violates SRP, but it's easier to experiment with performance.
internal class LongFileReaderKeepStream : IBytesProducer
{
   private readonly int _bufferSize;
   private readonly ILogger _logger;
   private readonly CancellationToken _cancellationToken;
   private readonly string _filePath;
   private readonly Encoding _encoding;
   private FileStream? _stream;
   private int _lastProcessedPackage;
   private readonly AsyncLock _lock;

   public LongFileReaderKeepStream(string fullFileName, Encoding encoding, int bufferSize, ILogger logger,
      CancellationToken cancellationToken)
   {
      _lock = new AsyncLock();
      _filePath = Guard.FileExist(fullFileName);
      _encoding = Guard.NotNull(encoding);
      _bufferSize = Guard.Positive(bufferSize);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   private async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset,
      CancellationToken cancellationToken)
   {
      //todo either make private or use another lock
      //it is save in the case of Rx, because it is always called from OnNextAsync
      // await using FileStream stream = File.OpenRead(_filePath);
      _stream ??= new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, true);
      
      int length = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
      return ReadingResult.Ok(length + offset, length);
   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      _stream ??= new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, false);
      
      int length = _stream.Read(buffer, offset, buffer.Length - offset);
      return ReadingResult.Ok(length + offset, length);
   }

   public async Task<ReadingPhasePackage> WriteBytesToBufferAsync(ReadingPhasePackage inputPackage)
   {
      await Task.Yield();
      ReadingResult result;

      using (var _ = await _lock.LockAsync())
      {
         if (inputPackage.PackageNumber != _lastProcessedPackage++)
            throw new InvalidOperationException("Wrong packages sequence.");

         result = await ReadBytesAsync(inputPackage.RowData, inputPackage.PrePopulatedBytesLength, _cancellationToken);
      }

      //todo handle in railway style 
      if (!result.Success)
         throw new InvalidOperationException(result.Message);

      var nextPackage = result.ActuallyRead == 0
         ? inputPackage with { IsLastPackage = true, WrittenBytesLength = result.Size }
         : inputPackage with { WrittenBytesLength = result.Size };
      return nextPackage;
   }

   public async ValueTask DisposeAsync()
   {
      if (_stream != null) await _stream.DisposeAsync();
   }

   public void Dispose()
   {
      _stream?.Dispose();
   }
}