
using System.Reactive.Subjects;
using System.Text;
using Infrastructure.Concurrency;
using Infrastructure.Parameters;
using LogsHub;
using OneOf;
using OneOf.Types;
using SortingEngine;
using SortingEngine.RowData;

namespace ConsoleWrapper.IOProcessing;

//todo rename
internal class LongFileReader : IBytesProducer, IAsyncDisposable
{
   private readonly ILogger _logger;
   private readonly CancellationToken _cancellationToken;
   private readonly string _fullFileName;
   private readonly Encoding _encoding;
   private FileStream _stream;
   private long _lastPosition;
   private int _lastProcessedPackage;
   private AsyncLock _lock;

   private readonly SimpleAsyncSubject<ReadingPhasePackage> _nextChunkPreparedSubject =
      new SequentialSimpleAsyncSubject<ReadingPhasePackage>();

   public IAsyncObservable<ReadingPhasePackage> NextChunkPrepared => _nextChunkPreparedSubject;

   public LongFileReader(string fullFileName, Encoding encoding, ILogger logger, CancellationToken cancellationToken)
   {
      _lock = new AsyncLock();
      _fullFileName = Guard.FileExist(fullFileName);
      _encoding = Guard.NotNull(encoding);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   // public Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer)
   // {
   //    throw new NotImplementedException();
   // }

   public async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset,
      CancellationToken cancellationToken)
   {
      //todo either make private or use another lock
      //it is save in the case of Rx, because it is always called from OnNextAsync
      await using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      await using RecordsReader reader = new RecordsReader(stream);
      var readingResult = await reader.ReadChunkAsync(buffer, offset, cancellationToken);
      if (!readingResult.Success)
         return readingResult;

      _lastPosition += readingResult.Size;
      return readingResult;
   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      using RecordsReader reader = new RecordsReader(stream);
      var readingResult = reader.ReadChunk(buffer, offset);
      if (!readingResult.Success)
         return readingResult;

      _lastPosition += readingResult.Size;
      return readingResult;
   }

   public ValueTask DisposeAsync()
   {
      return _stream?.DisposeAsync() ?? ValueTask.CompletedTask;
   }

   public async ValueTask OnNextAsync(ReadingPhasePackage inputPackage)
   {
      await Log($"Processing package: {inputPackage.PackageNumber}");
      ReadingResult result;

      using (var _ = await _lock.LockAsync())
      {
         //todo
         int num = inputPackage.PackageNumber;

         if (inputPackage.PackageNumber != _lastProcessedPackage++)
            throw new InvalidOperationException("Wrong packages sequence.");
         result = await ReadBytesAsync(inputPackage.RowData, inputPackage.PrePopulatedBytesLength, _cancellationToken);
      }
      //todo log
      if (!result.Success)
         await _nextChunkPreparedSubject.OnErrorAsync(new InvalidOperationException(result.Message));

      if (result.Size == 0)
      {
         await SendLastPackageAsync(inputPackage);
      }

      var nextPackage = inputPackage with { ReadBytesLength = result.Size };
      await _nextChunkPreparedSubject.OnNextAsync(nextPackage);
   }
   
   public async Task<ReadingPhasePackage> ProcessPackage(ReadingPhasePackage inputPackage)
   {
      await Log($"Processing package: {inputPackage.PackageNumber}");
      ReadingResult result;

      using (var _ = await _lock.LockAsync())
      {
         //todo
         int num = inputPackage.PackageNumber;

         if (inputPackage.PackageNumber != _lastProcessedPackage++)
            throw new InvalidOperationException("Wrong packages sequence.");
         result = await ReadBytesAsync(inputPackage.RowData, inputPackage.PrePopulatedBytesLength, _cancellationToken);
      }
      //todo log
      if (!result.Success)
         throw new InvalidOperationException(result.Message);

      if (result.Size == 0)
      {
         await SendLastPackageAsync(inputPackage);
      }

      var nextPackage = inputPackage with { ReadBytesLength = result.Size };
      return nextPackage;
   }

   private async ValueTask SendLastPackageAsync(ReadingPhasePackage package)
   {
      await Log($"Sending the last package: {package.PackageNumber} !!!");
      var nextPackage = package with { IsLastPackage = true};
      await _nextChunkPreparedSubject.OnNextAsync(nextPackage);
   }

   public ValueTask OnErrorAsync(Exception error)
   {
      return _nextChunkPreparedSubject.OnCompletedAsync();
   }

   public ValueTask OnCompletedAsync()
   {
      //we will complete this sequence as well, in such case there is nothing to do. Something went wrong
      return _nextChunkPreparedSubject.OnCompletedAsync();
   }
   
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }
}