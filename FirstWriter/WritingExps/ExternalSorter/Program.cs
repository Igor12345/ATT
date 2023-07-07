// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.CommandLine;

public static class Program
{
   public static async Task<int> Main(string[] args)
   {
      //https://learn.microsoft.com/en-us/dotnet/standard/commandline/

      var fileOption = new Option<FileInfo?>(
         name: "--file",
         description: "An option whose argument is parsed as a FileInfo",
         isDefault: true,
         parseArgument: result =>
         {
            if (result.Tokens.Count == 0)
            {
               return new FileInfo("sampleQuotes.txt");

            }
            string? filePath = result.Tokens.Single().Value;
            if (!File.Exists(filePath))
            {
               result.ErrorMessage = "File does not exist";
               return null;
            }
            else
            {
               return new FileInfo(filePath);
            }
         });

      var delayOption = new Option<int>(
         name: "--delay",
         description: "Delay between lines, specified as milliseconds per character in a line.",
         getDefaultValue: () => 42);

      var fgcolorOption = new Option<ConsoleColor>(
         name: "--fgcolor",
         description: "Foreground color of text displayed on the console.",
         getDefaultValue: () => ConsoleColor.White);

      var lightModeOption = new Option<bool>(
         name: "--light-mode",
         description: "Background color of text displayed on the console: default is black, light mode is white.");

      var rootCommand = new RootCommand("Sample app for System.CommandLine");

      var readCommand = new Command("read", "Read and display the file.")
      {
         fileOption,
         delayOption,
         fgcolorOption,
         lightModeOption
      };
      rootCommand.AddCommand(readCommand);

      readCommand.SetHandler(async (file, delay, fgcolor, lightMode) =>
         {
            await ReadFile(file!, delay, fgcolor, lightMode);
         },
         fileOption, delayOption, fgcolorOption, lightModeOption);

      return await rootCommand.InvokeAsync(args);
   }

   private static async Task ReadFile(FileInfo file, int delay, ConsoleColor fgcolor, bool lightMode)
   {
      Console.WriteLine("---------------");
      Console.WriteLine(file.FullName);
   }

   private static void ProcessFile(FileInfo file)
   {
      
   }
}

//request configuration

// byte[] buffer = ArrayPool<byte>.Shared.Rent(16000 * 8);