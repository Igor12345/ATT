namespace FileCreator.Configuration;

public interface IBaseConfiguration
{
    int? DuplicationFrequency { get; set; }
    string[]? Samples { get; set; }
    int? RandomSeed { get; set; }
    string? Encoding { get; set; }
    string? PossibleCharacters { get; set; }
    int? MaxLineLength { get; set; }
    int? MaxTextLength { get; set; }
    string? Delimiter { get; set; }
    string? OutputDirectory { get; set; }
    string? FileName { get; set; }
    string? FileSize { get; set; }
    int? LogEveryThsLine { get; set; }
}