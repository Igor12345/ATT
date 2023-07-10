using System.Buffers;
using System.Text;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SortingEngine;

internal class DataChunkManager : IAsyncDisposable
{
   private readonly Encoding _encoding;

   //todo keep open or reopen every time
   private readonly Stream _dataSource;
   private readonly Memory<byte> _rowStorage;
   private int _currentPosition;
   private readonly LineMemory[] _recordsStorage;
   private byte[]? _remainedBytes;
   private int _remindedBytesLength;
   private RecordsExtractor _extractor;
   private int _loadedLines;

   public DataChunkManager(string file, Memory<byte> rowStorage, LineMemory[] recordsStorage, Encoding encoding)
   {
      _rowStorage = rowStorage;
      _recordsStorage = recordsStorage;
      _encoding = encoding;
      _dataSource = File.OpenRead(file);
      _extractor =
         new RecordsExtractor(_encoding.GetBytes(Environment.NewLine), _encoding.GetBytes(". "));
   }

   public async IAsyncEnumerable<LineMemory> GetRecordsAsync()
   {
      _extractor =
         new RecordsExtractor(_encoding.GetBytes(Environment.NewLine), _encoding.GetBytes(". "));
      while (true)
      {
         int received = await _dataSource.ReadAsync(_rowStorage[..^_remindedBytesLength]);
         if (received == 0)
            break;
         _currentPosition = 0;
         //todo repetition
         ExtractionResult result = _extractor.SplitOnMemoryRecords(_rowStorage.Span, _recordsStorage);
         _remindedBytesLength = _rowStorage.Length - result.StartRemainingBytes;
         if (_remindedBytesLength > 0)
         {
            if (_remainedBytes != null && _remainedBytes.Length < _remindedBytesLength)
            {
               ArrayPool<byte>.Shared.Return(_remainedBytes);
               _remainedBytes = null;
            }

            _remainedBytes ??= ArrayPool<byte>.Shared.Rent(_remindedBytesLength);
            _rowStorage[result.StartRemainingBytes..].CopyTo(_rowStorage);
         }
         else
         {
            if (_remainedBytes != null)
            {
               ArrayPool<byte>.Shared.Return(_remainedBytes);
               _remainedBytes = null;
            }
         }

         if (result.Size >= _currentPosition)
            continue;
         yield return _recordsStorage[_currentPosition++];
      }
   }

   public async Task<(bool, LineMemory)> TryGetNextLineAsync()
   {
      if (NeedLoadLines())
      {
         ExtractionResult result = await LoadLinesAsync();
         if (result is { Success: true, Size: 0 })
         {
            return (false, default);
         }

         _loadedLines = result.Size;
         return (true, _recordsStorage[_currentPosition++]);
      }
      return (true, _recordsStorage[_currentPosition++]);
   }

   private bool NeedLoadLines()
   {
      return _currentPosition >= _loadedLines;
   }

   private async Task<ExtractionResult> LoadLinesAsync()
   {
      int received = await _dataSource.ReadAsync(_rowStorage[..^_remindedBytesLength]);
      if (received == 0)
         return ExtractionResult.Ok(0, -1);
      _currentPosition = 0;
      //todo repetition
      ExtractionResult result = _extractor.SplitOnMemoryRecords(_rowStorage.Span, _recordsStorage);
      _remindedBytesLength = _rowStorage.Length - result.StartRemainingBytes;
      if (_remindedBytesLength > 0)
      {
         if (_remainedBytes != null && _remainedBytes.Length < _remindedBytesLength)
         {
            ArrayPool<byte>.Shared.Return(_remainedBytes);
            _remainedBytes = null;
         }

         _remainedBytes ??= ArrayPool<byte>.Shared.Rent(_remindedBytesLength);
         _rowStorage[result.StartRemainingBytes..].CopyTo(_rowStorage);
      }
      else
      {
         if (_remainedBytes != null)
         {
            ArrayPool<byte>.Shared.Return(_remainedBytes);
            _remainedBytes = null;
         }
      }

      return result;
   }

   public ValueTask DisposeAsync()
   {
      if (_remainedBytes != null)
      {
         ArrayPool<byte>.Shared.Return(_remainedBytes);
      }

      _dataSource.Dispose();
      return ValueTask.CompletedTask;
   }
}