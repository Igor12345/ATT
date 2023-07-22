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
   private int _lastProcessedPackage;
   private readonly AsyncLock _lock;

   public LongFileReader(string fullFileName, int bufferSize, ILogger logger, CancellationToken cancellationToken)
   {
      _bufferSize = Guard.Positive(bufferSize);
      _lock = new AsyncLock();
      _filePath = Guard.FileExist(fullFileName);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   public Task<ReadingResult> ProvideBytesAsync(Memory<byte> buffer)
   {
      throw new NotImplementedException();
   }

   public ReadingResult ProvideBytes(Memory<byte> buffer)
   {
      throw new NotImplementedException();
   }

   private async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset,
      CancellationToken cancellationToken)
   {
      await using FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, true);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      int length = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
      
      _lastPosition += length;
      return ReadingResult.Ok(length + offset, length);
   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      using FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, false);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);
      
      int length = stream.Read(buffer, offset, buffer.Length - offset);

      _lastPosition += length;
      return ReadingResult.Ok(length + offset, length);
   }

   // public async Task<ReadyForExtractionPackage> WriteBytesToBufferAsync(ReadyForExtractionPackage inputPackage)
   // {
   //    await Task.Yield();
   //    ReadingResult result;
   //
   //    using (var _ = await _lock.LockAsync())
   //    {
   //       if (inputPackage.Id != _lastProcessedPackage++)
   //          throw new InvalidOperationException("Wrong packages sequence.");
   //
   //       result = await ReadBytesAsync(inputPackage.RowData, inputPackage.StartOfLine, _cancellationToken);
   //    }
   //
   //    //todo handle in railway style 
   //    if (!result.Success)
   //       throw new InvalidOperationException(result.Message);
   //
   //    ReadyForExtractionPackage nextPackage = result.ActuallyRead == 0
   //       ? inputPackage with { IsLastPackage = true, WrittenBytesLength = result.Size }
   //       : inputPackage with { WrittenBytesLength = result.Size };
   //    return nextPackage;
   // }

   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
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