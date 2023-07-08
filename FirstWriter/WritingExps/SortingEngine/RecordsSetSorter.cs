using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using InfoStructure.Parameters;
using OneOf;
using OneOf.Types;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.RuntimeConfiguration;
using SortingEngine.Sorters;

namespace SortingEngine
{
   public class RecordsSetSorter
   {
      private readonly Encoding _encoding;
      private byte[]? _inputBuffer;
      private IConfig _configuration;
      private PoolsManager _poolsManager = new PoolsManager();

      public RecordsSetSorter(Encoding encoding)
      {
         _encoding = Guard.NotNull(encoding, nameof(encoding));
      }

      public event EventHandler<SortingCompletedEventArgs> SortingCompleted;

      public async Task<Result> SortAsync(IBytesProducer producer, CancellationToken cancellationToken)
      {
         try
         {
            byte[] inputStorage = RentInputStorage();
            int length = 0;
            //make something more fancy
            while (length >= 0)
            {
               var result = await producer.PopulateAsync(inputStorage, cancellationToken);

               if (!result.Success)
               {
                  return new Result(false, result.Message);
               }

               length = result.Size;

               ProcessRecords(inputStorage);
            }

            return new Result(true, "");
         }
         catch (Exception e)
         {
            return new Result(false, e.Message);
         }
      }

      public async Task<OneOf<Success, Error<string>>> SortFuncAsync(IBytesProducer producer)
      {
         try
         {
            byte[] inputStorage = RentInputStorage();
            int length = 0;

            //make something more fancy
            while (length >= 0)
            {
               var result = await producer.PopulateAsyncFunc(inputStorage);
               string error = "";
               result.Switch(
                  r => length = r.Value,
                  e => error = e.Value);

               //todo !!! not functional
               if (!string.IsNullOrEmpty(error))
               {
                  return new Error<string>(error);
               }

               ProcessRecords(_inputBuffer);
            }

            return new Success();
         }
         catch (Exception e)
         {
            return new Error<string>(e.Message);
         }
      }

      private void ProcessRecords(byte[] inputBuffer)
      {
         RecordsExtractor extractor =
            new RecordsExtractor(_encoding.GetBytes(Environment.NewLine), _encoding.GetBytes(". "));
         LineMemory[] records = _poolsManager.AcquireRecordsArray();
         Result result = extractor.SplitOnMemoryRecords(inputBuffer, records);

         InSiteRecordsSorter sorter = new InSiteRecordsSorter(inputBuffer);
         LineMemory[] sorted = sorter.Sort(records);

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
   public record struct ReadingResult
   {
      public bool Success;
      public int Size;
      public string Message;
   }

   public class SortingCompletedEventArgs : EventArgs
   {
      public SortingCompletedEventArgs(LineMemory[] sorted, byte[] source)
      {
         Sorted = Guard.NotNull(sorted, nameof(sorted));
         Source = Guard.NotNull(source, nameof(source));
      }

      public LineMemory[] Sorted { get; init; }
      public byte[] Source { get; init; }
   }

   public record struct Result(bool Success, string Message);

   public interface IBytesProducer
   {
      Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer);
      Task<ReadingResult> PopulateAsync(byte[] buffer, CancellationToken cancellationToken);
   }

   public class PoolsManager
   {
      public LineMemory[] AcquireRecordsArray()
      {
         return new LineMemory[2_000_000];
      }
   }
}