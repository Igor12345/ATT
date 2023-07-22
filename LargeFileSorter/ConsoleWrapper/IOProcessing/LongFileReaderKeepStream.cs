using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

//This class violates SRP, but it's easier to experiment with performance.
internal class LongFileReaderKeepStream : IBytesProducer
{
   private readonly ILogger _logger;
   private readonly CancellationToken _cancellationToken;
   private FileStream _stream = null!;
   private readonly AsyncLock _lock;
   private readonly object _lockObj = new();

   private LongFileReaderKeepStream(ILogger logger,
      CancellationToken cancellationToken)
   {
      _lock = new AsyncLock();
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   public static IBytesProducer CreateForAsync(string filePath, int streamBufferSize, ILogger logger,
      CancellationToken cancellationToken)
   {
      //todo offset
      LongFileReaderKeepStream instance = new LongFileReaderKeepStream(logger, cancellationToken);
      instance.Init(filePath, streamBufferSize, true);
      return instance;
   }
   public static IBytesProducer CreateForSync(string filePath, int streamBufferSize, ILogger logger)
   {
      //todo
      Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) LongFileReaderKeepStream created");
      
      LongFileReaderKeepStream instance = new LongFileReaderKeepStream(logger, CancellationToken.None);
      instance.Init(filePath, streamBufferSize, false);
      return instance;
   }

   private void Init(string filePath, int bufferSize, bool useAsync)
   {
      filePath = Guard.FileExist(filePath);
      bufferSize = Guard.Positive(bufferSize);
      _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: bufferSize, useAsync);
   }

   public async Task<ReadingResult> ProvideBytesAsync(Memory<byte> buffer)
   {
      //todo
      Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) LongFileReaderKeepStream reading Async");
      using (AsyncLock.Releaser _ = await _lock.LockAsync())
      {
         int length = await _stream.ReadAsync(buffer, _cancellationToken);
         //todo
         Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) LongFileReaderKeepStream read {length} bytes Async");
         return ReadingResult.Ok(length);
      }
   }

   public ReadingResult ProvideBytes(Memory<byte> buffer)
   {
      //todo
      Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) LongFileReaderKeepStream reading Sync");
      lock (_lockObj)
      { 
         int length = _stream.Read(buffer.Span);
         //todo
         Console.WriteLine($"({Thread.CurrentThread.ManagedThreadId} at: {DateTime.Now:HH:mm:ss fff}) LongFileReaderKeepStream read {length} bytes Sync");
         return ReadingResult.Ok(length);
      }
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