using System.Text;
using SortingEngine.RuntimeConfiguration;

namespace ConsoleWrapper;

public class InputParametersValidator
{
   public (bool, ValidatedInputParameters) CheckInputParameters(InputParameters input)
   {
      if (!SelectFilePath(input, out string path)) 
         return (false, ValidatedInputParameters.Empty);

      if (!SelectEncoding(input, out Encoding encoding))
         return (false, ValidatedInputParameters.Empty);
      return (true, new ValidatedInputParameters(path, encoding));
   }

   private static bool SelectFilePath(InputParameters input, out string path)
   {
      path = input.File ?? "";

      while (true)
      {
         if (File.Exists(path))
            break;

         Console.WriteLine("Enter the filePath. Or 'X' to exit.");
         
         path = Console.ReadLine() ?? "";
         if (path.Equals( "X", StringComparison.InvariantCultureIgnoreCase))
            return false;

         if (string.IsNullOrEmpty(path)) continue;
         
         if (File.Exists(path))
            break;

         Console.WriteLine("File does not exist.");
      }

      return true;
   }

   private static bool SelectEncoding(InputParameters input, out Encoding encoding)
   {
      encoding = Encoding.Default;
      string? encodingName = input.Encoding;
      while (true)
      {
         if (TrySelectEncoding(encodingName, out encoding))
         {
            break;
         }

         Console.WriteLine("Enter encoding or Y if ASCII or X to exit.");
         encodingName = Console.ReadLine() ?? "";
         if (encodingName.ToUpper() == "X")
            return false;

         if (TrySelectEncoding(encodingName, out encoding))
            break;

         Console.WriteLine("Encoding does not exist");
      }

      return true;
   }

   private static bool TrySelectEncoding(string? encodingName, out Encoding encoding)
   {
      string? pattern = encodingName?.Replace(" ", "").Replace("-", "");
      if (string.Equals(pattern, "ASCII", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(pattern, "USASCII", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(pattern, "UTF8", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(pattern, "Y", StringComparison.OrdinalIgnoreCase))
      {
         encoding = Encoding.UTF8;
         return true;
      }

      if (string.Equals(pattern, "UTF16", StringComparison.OrdinalIgnoreCase)||
          string.Equals(pattern, "UTF16le", StringComparison.OrdinalIgnoreCase)||
          string.Equals(pattern, "Unicode", StringComparison.OrdinalIgnoreCase))
      {
         encoding = Encoding.Unicode;
         return true;
      }

      if (string.Equals(pattern, "UTF16be", StringComparison.OrdinalIgnoreCase))
      {
         encoding = Encoding.BigEndianUnicode;
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