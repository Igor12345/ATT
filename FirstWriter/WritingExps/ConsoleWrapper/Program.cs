using System.Text;
using SortingEngine;

namespace ConsoleWrapper
{
   internal class Program
   {
      static void Main(string[] args)
      {
         string path = "";
         string encodingName = "";
         while (true)
         {
            Console.WriteLine("Hi, enter the full file name. Or 'X' to exit.");
            path = Console.ReadLine() ?? "";

            if(path.ToUpper()=="X")
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

         while (true)
         {
            Console.WriteLine("Enter encoding or Y if ASCII or X to exit");
            encodingName = Console.ReadLine() ?? "";
            if (encodingName.ToUpper() == "X")
               return;
            if (TrySelectEncoding(encodingName, out Encoding encoding))
            {
               break;
            }

            Console.WriteLine("Encoding does not exist");
         }

         RecordsSetSorter sorter = new RecordsSetSorter();
      }

      private static bool TrySelectEncoding(string encodingName, out Encoding? encoding)
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

         encoding = null;
         return false;
      }
   }
}