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

   public async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset,
      CancellationToken cancellationToken)
   {
      //todo either make private or use another lock
      //it is save in the case of Rx, because it is always called from OnNextAsync
      // await using FileStream stream = File.OpenRead(_filePath);
      _stream ??= new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, true);
      
      int length = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
      return new ReadingResult { Success = true, Size = length + offset };

   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      _stream ??= new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None,
         bufferSize: _bufferSize, false);
      
      int length = _stream.Read(buffer, offset, buffer.Length - offset);
      return new ReadingResult { Success = true, Size = length + offset };
   }

   public async Task<ReadingPhasePackage> ProcessPackageAsync(ReadingPhasePackage inputPackage)
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

   public async ValueTask DisposeAsync()
   {
      if (_stream != null) await _stream.DisposeAsync();
   }

   public void Dispose()
   {
      _stream?.Dispose();
   }
}