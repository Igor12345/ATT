using System.Text;
using SortingEngine.RuntimeConfiguration;

namespace ConsoleWrapper;

public class InputParametersValidator
{

   public (bool, ValidatedInputParameters) CheckInputParameters(InputParameters input)
   {
      string path = input.File ?? "";

      while (true)
      {
         if (File.Exists(path))
         {
            break;
         }
         //todo
         Console.WriteLine("Hi, enter the full name of the file. Or 'X' to exit.");
         //todo
         path = Console.ReadLine() ?? "";
         if (path.ToUpper() == "X")
            return (false, ValidatedInputParameters.Empty);
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
      string encodingName = input.Encoding;
      while (true)
      {
         if (TrySelectEncoding(encodingName, out encoding))
         {
            break;
         }

         Console.WriteLine("Enter encoding or Y if ASCII or X to exit");
         encodingName = Console.ReadLine() ?? "";
         if (encodingName.ToUpper() == "X")
            return (false, ValidatedInputParameters.Empty);
         if (TrySelectEncoding(encodingName, out encoding))
         {
            break;
         }

         Console.WriteLine("Encoding does not exist");
      }
      return (true, new ValidatedInputParameters(path, encoding));
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