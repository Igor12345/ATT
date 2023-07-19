using System.Text;

namespace FileCreator.Configuration;

public sealed class RuntimeConfiguration : IRuntimeConfiguration
{
    
    //check it should be less then Samples
    public int DuplicationFrequency { get; init; } = 1000;
    public string[] Samples { get; init; } = Array.Empty<string>();
    public int Seed { get; init; } = 42;
    public Encoding Encoding { get; init; } = Encoding.UTF8;
    public string PossibleCharacters { get; init; } =
        @"AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz_+-<>/?!@#*&()""";
    
    public string Delimiter { get; init; } = ". ";
    public int MaxTextLength { get; init; } = 1000;
    public int MaxLineLength { get; init; } = 1024;
    
    //name of the file, if something is wrong with the folder,
    //it will be created in the current directory
    public string FilePath { get; init; } = "BigFile"; 
    public ulong FileSize { get; init; } = 1_073_741_824; //1 Gb
    public int LogEveryThsLine { get; init; } = 10_000;
}