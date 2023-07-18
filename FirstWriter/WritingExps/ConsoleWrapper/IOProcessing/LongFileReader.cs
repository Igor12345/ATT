
using System.Reactive.Subjects;
using System.Text;
using Infrastructure.ByteOperations;
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
   private readonly AsyncLock _lock;

   public LongFileReader(string fullFileName, Encoding encoding, ILogger logger, CancellationToken cancellationToken)
   {
      _lock = new AsyncLock();
      _fullFileName = Guard.FileExist(fullFileName);
      _encoding = Guard.NotNull(encoding);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
   }

   public async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset,
      CancellationToken cancellationToken)
   {
      //todo either make private or use another lock
      //it is save in the case of Rx, because it is always called from OnNextAsync
      await using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      await using LinesReader reader = new LinesReader(stream);
      var readingResult = await reader.ReadChunkAsync(buffer, offset, cancellationToken);
      if (!readingResult.Success)
         return readingResult;
      
      _lastPosition += readingResult.Size-offset;
      return readingResult;
   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      using LinesReader reader = new LinesReader(stream);
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

   public async Task<ReadingPhasePackage> ProcessPackage(ReadingPhasePackage inputPackage)
   {
      await Task.Yield();
         //todo
      //    int num = inputPackage.PackageNumber;
      //    int wl = inputPackage.WrittenBytesLength;
      //    int pb = inputPackage.PrePopulatedBytesLength;
      // int id = inputPackage.RowData.GetHashCode();
      // await Log($"Processing package: {inputPackage.PackageNumber}, is last: {inputPackage.IsLastPackage}, " +
      //           $"bufferId: {id}, contains bytes: {inputPackage.WrittenBytesLength}, " +
      //           $"already populated: {inputPackage.PrePopulatedBytesLength}, thread: {Thread.CurrentThread.ManagedThreadId}");
      ReadingResult result;

      using (var _ = await _lock.LockAsync())
      {
         // Console.WriteLine($"LongFileReader.ProcessPackage; lock passed package: {inputPackage.PackageNumber}, thread: {Thread.CurrentThread.ManagedThreadId} ");
         
         if (inputPackage.PackageNumber != _lastProcessedPackage++)
            throw new InvalidOperationException("Wrong packages sequence.");
         
         // Console.WriteLine($"Reading new package {inputPackage.PackageNumber}, tail from last ({inputPackage.PrePopulatedBytesLength} byte): ");
         // Console.WriteLine($"Tail {ByteToStringConverter.Convert(inputPackage.RowData.AsSpan()[..inputPackage.PrePopulatedBytesLength])}");
         // Console.WriteLine($"Package {inputPackage.PackageNumber}, rowData length: {inputPackage.RowData.Length}");
         // Console.WriteLine("Reading ----- ");
         
         result = await ReadBytesAsync(inputPackage.RowData, inputPackage.PrePopulatedBytesLength, _cancellationToken);
         
         // Console.WriteLine($"Tail+next ({inputPackage.PackageNumber}-{inputPackage.PrePopulatedBytesLength}): {ByteToStringConverter.Convert(inputPackage.RowData.AsSpan()[..(2*inputPackage.PrePopulatedBytesLength)])}");
      }

      // Console.WriteLine(
      //    $"LongFileReader.ProcessPackage; After  package: {inputPackage.PackageNumber}, " +
      //    $"thread: {Thread.CurrentThread.ManagedThreadId}, reading result: {result.Success}, read bytes: {result.Size} ");
      //todo handle in railway style 
      if (!result.Success)
         throw new InvalidOperationException(result.Message);

      // if (result.Size == 0)
      // {
      //    id = inputPackage.RowData.GetHashCode();
      //    await Log($"Sending the last package: {inputPackage.PackageNumber} !!! " +
      //              $"bufferId: {id}, contains bytes: {result.Size}, thread: {Thread.CurrentThread.ManagedThreadId}");
      // }

      var nextPackage = result.Size == 0
         ? inputPackage with { IsLastPackage = true, WrittenBytesLength = 0 }
         : inputPackage with { WrittenBytesLength = result.Size };
      return nextPackage;
   }

   // public ValueTask OnErrorAsync(Exception error)
   // {
   //    // return _nextChunkPreparedSubject.OnCompletedAsync();
   // }
   //
   // public ValueTask OnCompletedAsync()
   // {
   //    //we will complete this sequence as well, in such case there is nothing to do. Something went wrong
   //    // return _nextChunkPreparedSubject.OnCompletedAsync();
   // }
   
   private async ValueTask Log(string message)
   {
      //in the real projects it will be structured logs
      string prefix = $"{this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
      await _logger.LogAsync(prefix + message);
   }
}