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

   public DataChunkManager(string file, Memory<byte> rowStorage, LineMemory[] recordsStorage)
   {
      _rowStorage = rowStorage;
      _recordsStorage = recordsStorage;
      _dataSource = File.OpenRead(file);
   }

   public async IAsyncEnumerable<LineMemory> GetRecordsAsync()
   {
      RecordsExtractor extractor =
         new RecordsExtractor(_encoding.GetBytes(Environment.NewLine), _encoding.GetBytes(". "));
      while (true)
      {
         int received = await _dataSource.ReadAsync(_rowStorage[..^_remindedBytesLength]);
         if (received == 0)
            break;
         _currentPosition = 0;
         //todo repetition
         ExtractionResult result = extractor.SplitOnMemoryRecords(_rowStorage.Span, _recordsStorage);
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

   public ValueTask DisposeAsync()
   {
      if (_remainedBytes != null)
      {
         ArrayPool<byte>.Shared.Return(_remainedBytes);
      }

      _dataSource?.Dispose();
      return ValueTask.CompletedTask;
   }

   public async Task<LineMemory> GetNextLineAsync()
   {
      await foreach (LineMemory line in GetRecordsAsync())
         yield return line;
      yield break;
   }
}