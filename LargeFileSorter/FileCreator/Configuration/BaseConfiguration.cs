namespace FileCreator.Configuration;

public sealed class BaseConfiguration : IBaseConfiguration
{
    public int? DuplicationFrequency { get; set; }
    public string[]? Samples { get; set; }
    public int? RandomSeed { get; set; }
    public string? Encoding { get; set; }
    public string? PossibleCharacters { get; set; }
    public int? MaxLineLength { get; set; }
    public int? MaxTextLength { get; set; }
    public string? Delimiter { get; set; }
    public string? OutputDirectory { get; set; }
    public string? FileName { get; set; }
    public string? FileSize { get; set; }
    public int? LogEveryThsLine { get; set; }
}