using System.Buffers;
using OneOf;
using OneOf.Types;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.Sorters;

namespace SortingEngine
{
   //todo dispose, return _remainingBytes
   public class RecordsSetSorter
   {
      private byte[]? _inputBuffer;
      private IConfig _configuration;
      private PoolsManager _poolsManager;
      private byte[]? _remainedBytes;
      private int _remindedBytesLength;

      public event EventHandler<SortingCompletedEventArgs>? SortingCompleted;

      public event EventHandler<SortingCompletedEventArgs>? OutputBufferFull;

      public RecordsSetSorter(IConfig configuration)
      {
         _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
      }

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
               if (_remindedBytesLength > 0)
               {
                  _remainedBytes!.CopyTo(inputStorage, 0);
               }

               ReadingResult result =
                  await producer.ReadBytesAsync(inputStorage, _remindedBytesLength, cancellationToken);

               if (!result.Success)
               {
                  return new Result(false, result.Message);
               }

               if (result.Size == 0)
                  break;

               length = result.Size;

               var slice = inputStorage.AsMemory()[..result.Size];
               ProcessRecords(slice);
            }

            _inputBuffer = null;
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
         StreamsMergeExecutor merger = new StreamsMergeExecutor(_configuration);
         merger.OutputBufferFull += (o, eventArgs) => MergerOnOutputBufferFull(o, eventArgs);
         var result = await merger.MergeWithOrder();
         return result;
      }

      private void MergerOnOutputBufferFull(object? sender, SortingCompletedEventArgs e)
      {
         OnOutputBufferFull(e);
      }

      private void Init()
      {
         _poolsManager = new PoolsManager(10, _configuration.RecordsBufferSize);
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
            new RecordsExtractor(_configuration.Encoding.GetBytes(Environment.NewLine),
               _configuration.Encoding.GetBytes(". "));
         LineMemory[] sorted;

         using ExpandingStorage<LineMemory> recordsStorage =
            new ExpandingStorage<LineMemory>(_configuration.RecordsBufferSize);

         //todo array vs slice
         ExtractionResult result = extractor.SplitOnMemoryRecords(inputBuffer.Span, recordsStorage);

         if (!result.Success)
         {
            //todo railway
            throw new InvalidOperationException(result.Message);
         }

         _remindedBytesLength = inputBuffer.Length - result.StartRemainingBytes;
         if (_remindedBytesLength > 0)
         {
            _remainedBytes ??= ArrayPool<byte>.Shared.Rent(Constants.MaxTextLength);
            inputBuffer.Span[result.StartRemainingBytes..].CopyTo(_remainedBytes);
         }

         InSiteRecordsSorter sorter = new InSiteRecordsSorter(inputBuffer);
         sorted = sorter.Sort(recordsStorage, result.Size);

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

      protected virtual void OnOutputBufferFull(SortingCompletedEventArgs e)
      {
         OutputBufferFull?.Invoke(this, e);
      }
   }
}