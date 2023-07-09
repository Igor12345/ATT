using System.Buffers;
using System.Text;
using Infrastructure.Parameters;
using OneOf;
using OneOf.Types;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.RuntimeEnvironment;
using SortingEngine.Sorters;

namespace SortingEngine
{
   //todo dispose, return _remainingBytes
   public class RecordsSetSorter
   {
      private readonly Encoding _encoding;
      private byte[]? _inputBuffer;
      private IConfig _configuration;
      private PoolsManager _poolsManager = new PoolsManager();
      private byte[]? _remainedBytes;
      private int _remindedBytesLength;

      public RecordsSetSorter(Encoding encoding)
      {
         _encoding = Guard.NotNull(encoding, nameof(encoding));
      }

      public event EventHandler<SortingCompletedEventArgs> SortingCompleted;

      public async Task<Result> SortAsync(IBytesProducer producer, CancellationToken cancellationToken)
      {
         Init();

         try
         {
            //todo check filesize and write to final if small

            byte[] inputStorage = RentInputStorage();
            int length = 1;

            //split stage

            //make something more fancy
            while (length > 0)
            {
               if (_remainedBytes != null)
               {
                  _remainedBytes.CopyTo(inputStorage, 0);
               }

               ReadingResult result = await producer.ReadBytesAsync(inputStorage, _remindedBytesLength, cancellationToken);

               if (!result.Success)
               {
                  return new Result(false, result.Message);
               }

               if(result.Size==0)
                  break;

               length = result.Size;

               var slice = inputStorage.AsMemory()[..result.Size];
               ProcessRecords(slice);
            }

            _inputBuffer = null;
            _remainedBytes = null;
            _poolsManager.DeleteArrays();

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            //merge stage
            await MergeToOneFileAsync();

            return new Result(true, "");
         }
         catch (Exception e)
         {
            return new Result(false, e.Message);
         }
      }

      private async Task<Result> MergeToOneFileAsync()
      {
         FilesMerger merger = new FilesMerger();
      }

      private void Init()
      {
         IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer();
         _configuration = analyzer.SuggestConfig();
      }

      // public async Task<OneOf<Success, Error<string>>> SortFuncAsync(IBytesProducer producer)
      // {
      //    try
      //    {
      //       byte[] inputStorage = RentInputStorage();
      //       int length = 0;
      //
      //       //make something more fancy
      //       while (length >= 0)
      //       {
      //          var result = await producer.PopulateAsyncFunc(inputStorage);
      //          string error = "";
      //          result.Switch(
      //             r => length = r.Value,
      //             e => error = e.Value);
      //
      //          //todo !!! not functional
      //          if (!string.IsNullOrEmpty(error))
      //          {
      //             return new Error<string>(error);
      //          }
      //
      //          ProcessRecords(_inputBuffer);
      //       }
      //
      //       return new Success();
      //    }
      //    catch (Exception e)
      //    {
      //       return new Error<string>(e.Message);
      //    }
      // }

      private void ProcessRecords(ReadOnlyMemory<byte> inputBuffer)
      {
         RecordsExtractor extractor =
            new RecordsExtractor(_encoding.GetBytes(Environment.NewLine), _encoding.GetBytes(". "));
         LineMemory[] sorted;
         LineMemory[]? records = null;
         try
         {
            records = _poolsManager.AcquireRecordsArray();

            //todo array vs slice
            ExtractionResult result = extractor.SplitOnMemoryRecords(inputBuffer.Span, records);

            if (!result.Success)
            {
               //todo
               throw new InvalidOperationException(result.Message);
            }

            _remindedBytesLength = inputBuffer.Length - result.StartRemainingBytes;
            if (_remindedBytesLength > 0)
            {
               if (_remainedBytes != null && _remainedBytes.Length < _remindedBytesLength)
               {
                  ArrayPool<byte>.Shared.Return(_remainedBytes);
                  _remainedBytes = null;
               }

               _remainedBytes ??= ArrayPool<byte>.Shared.Rent(_remindedBytesLength);
               inputBuffer.Span[result.StartRemainingBytes..].CopyTo(_remainedBytes);
            }
            else
            {
               if (_remainedBytes != null)
               {
                  ArrayPool<byte>.Shared.Return(_remainedBytes);
                  _remainedBytes = null;
               }
            }

            InSiteRecordsSorter sorter = new InSiteRecordsSorter(inputBuffer);
            sorted = sorter.Sort(records[..result.Size]);
         }
         finally
         {
            _poolsManager.Return(records);
         }
         OnSortingCompleted(new SortingCompletedEventArgs(sorted, inputBuffer));
      }

      private byte[] RentInputStorage()
      {
         //todo introduce buffer manager
         _inputBuffer ??= new byte[_configuration.InputBufferSize];
         return _inputBuffer;
      }

      private void OnSortingCompleted(SortingCompletedEventArgs e)
      {
         SortingCompleted?.Invoke(this, e);
      }
   }
}