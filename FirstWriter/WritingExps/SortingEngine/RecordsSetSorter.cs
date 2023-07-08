using System.Diagnostics;
using OneOf;
using OneOf.Types;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine
{
   public class RecordsSetSorter
   {
      private byte[]? _inputBuffer;
      private IConfig _configuration;

      public async Task<Result> SortAsync(IBytesProducer producer)
      {
         try
         {
            byte[] inputStorage = RentInputStorage();
            int length = 0;

            //make something more fancy
            while (length >= 0)
            {
               var result = await producer.PopulateAsync(inputStorage);

               if (!result.Success)
               {
                  return new Result(false, result.Message);
               }

               length = result.Size;

               ProcessRecords(_inputBuffer);
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

      }

      private byte[] RentInputStorage()
      {
         //todo introduce buffer manager
         _inputBuffer ??= new byte[_configuration.InputBufferSize];
         return _inputBuffer;
      }
   }
   public record struct ReadingResult
   {
      public bool Success;
      public int Size;
      public string Message;
   }

   public record struct Result(bool Success, string Message);

   public interface IBytesProducer
   {
      Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer);
      Task<ReadingResult> PopulateAsync(byte[] buffer);
   }
}