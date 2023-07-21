using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

//This class violates SRP, but it's easier to experiment with performance.
internal class LongFileReaderKeepStream : IBytesProducer
{
   private readonly int _offset;
   private readonly ILogger _logger;
   private readonly CancellationToken _cancellationToken;
   private FileStream _stream = null!;
   private int _lastProcessedPackage;
   private readonly AsyncLock _lock;
   private bool _useAsync;

   private LongFileReaderKeepStream(int offset, ILogger logger,
      CancellationToken cancellationToken)
   {
      _lock = new AsyncLock();
      _offset = Guard.Positive(offset);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   public static IBytesProducer CreateForAsync(string filePath, int streamBufferSize, ILogger logger,
      CancellationToken cancellationToken)
   {
      //todo offset
      LongFileReaderKeepStream instance = new LongFileReaderKeepStream(0, logger, cancellationToken);
      instance.Init(filePath, streamBufferSize, true);
      return instance;
   }
   public static IBytesProducer CreateForSync(string filePath, int offset, int streamBufferSize, ILogger logger)
   {
      LongFileReaderKeepStream instance = new LongFileReaderKeepStream(offset, logger, CancellationToken.None);
      instance.Init(filePath, streamBufferSize, false);
      return instance;
   }

   private void Init(string filePath, int bufferSize, bool useAsync)
   {
      _useAsync = useAsync;
      filePath = Guard.FileExist(filePath);
      bufferSize = Guard.Positive(bufferSize);
      _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: bufferSize, useAsync);
   }

   private async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset)
   {
      //todo either make private or use another lock
      //it is save in the case of Rx, because it is always called from OnNextAsync
      // await using FileStream stream = File.OpenRead(_filePath);
      
      int length = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), _cancellationToken);
      return ReadingResult.Ok(length + offset, length);
   }

   public ReadingResult ReadBytes(Span<byte> buffer)
   {
      int length = _stream.Read(buffer);
      return ReadingResult.Ok(length, length);
   }

   public async Task<ReadyForExtractionPackage> WriteBytesToBufferAsync(ReadyForExtractionPackage inputPackage)
   {
      await Task.Yield();
      ReadingResult result;

      using (var _ = await _lock.LockAsync())
      {
         if (inputPackage.Id != _lastProcessedPackage++)
            throw new InvalidOperationException("Wrong packages sequence.");

         result = _useAsync
            ? await ReadBytesAsync(inputPackage.RowData, inputPackage.StartOfLine)
            : ReadBytes(inputPackage.RowData.AsSpan(_offset..));
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
      await _stream.DisposeAsync();
   }

   public void Dispose()
   {
      _stream.Dispose();
   }
}