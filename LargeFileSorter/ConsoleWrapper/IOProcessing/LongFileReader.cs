using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

//This class violates SRP, but it's easier to experiment with performance.
internal class LongFileReader : IBytesProducer
{
   private readonly int _bufferSize;
   private readonly ILogger _logger;
   private readonly CancellationToken _cancellationToken;
   private readonly string _filePath;
   private long _lastPosition;
   private readonly AsyncLock _lock;
   private readonly object _lockObj = new();

   public LongFileReader(string fullFileName, int bufferSize, ILogger logger, CancellationToken cancellationToken)
   {
      _bufferSize = Guard.Positive(bufferSize);
      _lock = new AsyncLock();
      _filePath = Guard.FileExist(fullFileName);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   public async Task<ReadingResult> ProvideBytesAsync(Memory<byte> buffer)
   {
      using var _ = await _lock.LockAsync();
      await using FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, true);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      int length = await stream.ReadAsync(buffer, _cancellationToken);
      
      _lastPosition += length;
      return ReadingResult.Ok( length);
   }

   public ReadingResult ProvideBytes(Memory<byte> buffer)
   {
      lock (_lockObj)
      {
         using FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
            bufferSize: _bufferSize, false);
         if (_lastPosition > 0)
            stream.Seek(_lastPosition, SeekOrigin.Begin);

         int length = stream.Read(buffer.Span);

         _lastPosition += length;
         return ReadingResult.Ok(length);
      }
   }

   //only for experiments, that is faster, keep open stream or create it when necessary
   //another class is LongFileReaderKeepStream
   public  ValueTask DisposeAsync()
   {
      return ValueTask.CompletedTask;
   }

   public void Dispose()
   {
   }
}