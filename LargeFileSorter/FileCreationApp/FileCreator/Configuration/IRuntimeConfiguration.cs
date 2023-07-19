using System.Text;

namespace FileCreator.Configuration;

public interface IRuntimeConfiguration
{
    int DuplicationFrequency { get; init; }
    string[] Samples { get; init; }
    int Seed { get; init; }
    Encoding Encoding { get; init; }
    string PossibleCharacters { get; init; }
    string Delimiter { get; init; }
    int MaxTextLength { get; init; }
    int MaxLineLength { get; init; }
    string FilePath { get; init; }
    ulong FileSize { get; init; } //1 Gb
    int LogEveryThsLine { get; init; }
}