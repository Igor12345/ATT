using System.Diagnostics;
using System.Text;
using ConsoleWrapper.IOProcessing;
using SortingEngine;
using SortingEngine.RuntimeEnvironment;

namespace ConsoleWrapper
{
   internal class Program
   {
      static async Task Main(string[] args)
      {
         //todo!!! handle lack eol on the last line
         //do not split small files

         string path = "";
         while (true)
         {
            Console.WriteLine("Hi, enter the full name of the file. Or 'X' to exit.");
            //todo
            path = Console.ReadLine() ?? "";
            path = @"d:\\Temp\\ATT\\onlyLetters_middle2";
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
         
         IEnvironmentAnalyzer analyzer = new EnvironmentAnalyzer();
         var configuration = analyzer.SuggestConfig(path, encoding);

         RecordsSetSorter sorter = new RecordsSetSorter(configuration);
         IntermediateResultsDirector chunksDirector =
            IntermediateResultsDirector.Create(configuration.TemporaryFolder, cts.Token);
         await using ResultWriter resultWriter = ResultWriter.Create(path, cts.Token);
         sorter.SortingCompleted += (o, eventArgs) => chunksDirector.WriteRecords(eventArgs);
         sorter.OutputBufferFull += (o, eventArgs) => resultWriter.WriteOutput(eventArgs);
         IBytesProducer bytesReader = new LongFileReader(path, encoding);

         Stopwatch sw = Stopwatch.StartNew();

         var result = await sorter.SortAsync(bytesReader, cts.Token);

         sw.Stop();
         Console.WriteLine(result.Success
            ? $"---> Success - {sw.Elapsed.TotalMinutes} min, {sw.Elapsed.Seconds} sec; Total: {sw.Elapsed.TotalSeconds} sec, {sw.Elapsed.TotalMilliseconds} ms"
            : $"---> Error: {result.Message}");
      }

      private static void SortingCompleted(object? sender, SortingCompletedEventArgs e)
      {
         throw new NotImplementedException();
      }

      //todo another class
      private static bool TrySelectEncoding(string encodingName, out Encoding encoding)
      {
         if (string.Equals(encodingName, "ASCII", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(encodingName, "UTF8", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(encodingName, "Y", StringComparison.OrdinalIgnoreCase))
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