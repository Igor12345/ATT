using System.Buffers;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SortingEngine.Merging;

internal class DataChunkManager : IDisposable
{
   private readonly Stream _dataSource;
   private readonly Memory<byte> _rowStorage;
   private readonly int _offset;
   private int _currentPosition;
   private readonly ExpandingStorage<Line> _recordsStorage;
   private byte[]? _remainedBytes;
   private int _remindedBytesLength;
   private readonly int _remindedBytesCapacity;
   private readonly Func<Result> _flushOutputBuffer;
   private readonly LinesExtractor _extractor;
   private int _loadedLines;

   public DataChunkManager(Func<Stream> dataStreamFactory, Memory<byte> rowStorage, int offset, LinesExtractor extractor,
      ExpandingStorage<Line> recordsStorage, int maxLineLength, Func<Result> flushOutputBuffer)
   {
      _rowStorage = NotNull(rowStorage);
      _offset = offset;
      _recordsStorage = NotNull(recordsStorage);
      _dataSource = dataStreamFactory();
      _remindedBytesCapacity = maxLineLength;
      _flushOutputBuffer = NotNull(flushOutputBuffer);
      _extractor = NotNull(extractor);
   }

   public (ExtractionResult, bool, Line) TryGetNextLine()
   {
      if (NeedLoadLines())
      {
         ExtractionResult result = LoadLines();
         if (result is { Success: true, LinesNumber: 0 })
         {
            return (result, false, default);
         }

         if (!result.Success)
            return (result, false, default);

         _loadedLines = result.LinesNumber;
         return (result, true, _recordsStorage[_currentPosition++]);
      }

      //todo useless result
      return (ExtractionResult.Ok(0,0), true, _recordsStorage[_currentPosition++]);
   }

   private bool NeedLoadLines()
   {
      return _currentPosition >= _loadedLines;
   }

   private ExtractionResult LoadLines()
   {
      Result flushBufferResult = _flushOutputBuffer();
      if (!flushBufferResult.Success)
         return ExtractionResult.Error(flushBufferResult.Message);
      
      _recordsStorage.Clear();

      if (_remindedBytesLength > 0)
         _remainedBytes.CopyTo(_rowStorage);

      int received = _dataSource.Read(_rowStorage.Span[_remindedBytesLength..]);
      if (received == 0)
         return ExtractionResult.Ok(0, -1);
      _currentPosition = 0;
      
      int recognizableBytes = (_remindedBytesLength + received);
      ExtractionResult result =
         _extractor.ExtractRecords(_rowStorage.Span[..recognizableBytes], _recordsStorage, _offset);

      //todo convert all to the railway style 
      if (!result.Success)
         return result;

      if (result.LinesNumber == 0)
         return result;

      _remindedBytesLength = recognizableBytes - result.StartRemainingBytes;
      if (_remindedBytesLength > 0)
      {
         _remainedBytes ??= ArrayPool<byte>.Shared.Rent(_remindedBytesCapacity);
         _rowStorage.Span[result.StartRemainingBytes..].CopyTo(_remainedBytes);
      }

      return result;
   }

   public void Dispose()
   {
      if (_remainedBytes != null)
         ArrayPool<byte>.Shared.Return(_remainedBytes);
      _dataSource.Dispose();
      _recordsStorage.Dispose();
   }
}