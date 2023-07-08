using System.Text;
using ConsoleWrapper.IOProcessing;
using SortingEngine;

namespace ConsoleWrapper
{
    internal class Program
   {
      static async Task Main(string[] args)
      {
         string path = "";
         while (true)
         {
            Console.WriteLine("Hi, enter the full name of the file. Or 'X' to exit.");
            path = Console.ReadLine() ?? "";

            if (path.ToUpper() == "X")
               return;
            if (!string.IsNullOrEmpty(path))
            {
               if (File.Exists(path))
               {
                  break;
               }

               Console.WriteLine("File does not exist");
            }
         }

         Encoding encoding;
         while (true)
         {
            Console.WriteLine("Enter encoding or Y if ASCII or X to exit");
            var encodingName = Console.ReadLine() ?? "";
            if (encodingName.ToUpper() == "X")
               return;
            if (TrySelectEncoding(encodingName, out encoding))
            {
               break;
            }

            Console.WriteLine("Encoding does not exist");
         }

         CancellationTokenSource cts = new CancellationTokenSource();

         RecordsSetSorter sorter = new RecordsSetSorter(encoding);
         IntermediateResultsDirector chunksDirector = IntermediateResultsDirector.Create(path, cts.Token);
         sorter.SortingCompleted += (o, eventArgs) => chunksDirector.WriteRecordsAsync(o, eventArgs).Wait();
         IBytesProducer bytesReader = new LongFileReader(path, encoding);
         var result = await sorter.SortAsync(bytesReader, cts.Token);

         Console.WriteLine(result.Success ? "Success" : $"Error: {result.Message}");
      }

      private static void SortingCompleted(object? sender, SortingCompletedEventArgs e)
      {
         throw new NotImplementedException();
      }

      //todo another class
      private static bool TrySelectEncoding(string encodingName, out Encoding encoding)
      {
         if (string.Equals(encodingName, "ASCII", StringComparison.OrdinalIgnoreCase))
         {
            encoding = Encoding.UTF8;
            return true;
         }
         if (string.Equals(encodingName, "UTF8", StringComparison.OrdinalIgnoreCase))
         {
            encoding = Encoding.UTF8;
            return true;
         }
         if (string.Equals(encodingName, "UTF32", StringComparison.OrdinalIgnoreCase))
         {
            encoding = Encoding.UTF32;
            return true;
         }

         encoding = Encoding.Default;
         return false;
      }
   }
}