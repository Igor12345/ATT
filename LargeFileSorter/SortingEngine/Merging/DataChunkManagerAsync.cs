using System.Buffers;
using Infrastructure.Parameters;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SortingEngine.Merging;

internal class DataChunkManagerAsync : IAsyncDisposable
{
   private readonly Stream _dataSource;
   private readonly Memory<byte> _rowStorage;
   private readonly int _offset;
   private int _currentPosition;
   private readonly ExpandingStorage<Line> _recordsStorage;
   private byte[]? _remainedBytes;
   private int _remindedBytesLength;
   private readonly int _remindedBytesCapacity;
   private readonly LinesExtractor _extractor;
   private int _loadedLines;
   private readonly CancellationToken _token;

   public DataChunkManagerAsync(string file, Memory<byte> rowStorage, int offset, LinesExtractor extractor,
      Func<ExpandingStorage<Line>> recordsStorageProvider, int maxLineLength, CancellationToken token)
   {
      _rowStorage = rowStorage;
      _offset = offset;
      _recordsStorage = recordsStorageProvider();
      //todo
      _dataSource = File.OpenRead(file);
      _dataSource = File.OpenRead(file);
      _remindedBytesCapacity = maxLineLength;
      _extractor = Guard.NotNull(extractor);
      _token = Guard.NotNull(token);
   }
   
   public async Task<(ExtractionResult, bool, Line)> TryGetNextLineAsync()
   {
      if (NeedLoadLines())
      {
         ExtractionResult result = await LoadLinesAsync();
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

   private async Task<ExtractionResult> LoadLinesAsync()
   {
      _recordsStorage.Clear();

      if (_remindedBytesLength > 0)
      {
         _remainedBytes.CopyTo(_rowStorage);
      }

      int received = await _dataSource.ReadAsync(_rowStorage[_remindedBytesLength..]);
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

   public ValueTask DisposeAsync()
   {
      if (_remainedBytes != null)
         ArrayPool<byte>.Shared.Return(_remainedBytes);

      return _dataSource.DisposeAsync();
   }
}