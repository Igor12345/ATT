using System.Text;
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
   private readonly Encoding _encoding;
   private long _lastPosition;
   private int _lastProcessedPackage;
   private readonly AsyncLock _lock;

   public LongFileReader(string fullFileName, Encoding encoding, int bufferSize, ILogger logger, CancellationToken cancellationToken)
   {
      _bufferSize = Guard.Positive(bufferSize);
      _lock = new AsyncLock();
      _filePath = Guard.FileExist(fullFileName);
      _encoding = Guard.NotNull(encoding);
      _logger = Guard.NotNull(logger);
      _cancellationToken = Guard.NotNull(cancellationToken);
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
      return new ReadingResult { Success = true, Size = length + offset };
   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      using FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, false);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);
      
      int length = stream.Read(buffer, offset, buffer.Length - offset);

      _lastPosition += length;
      return new ReadingResult { Success = true, Size = length + offset };
   }

   public async Task<ReadingPhasePackage> ProcessPackageAsync(ReadingPhasePackage inputPackage)
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