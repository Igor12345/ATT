namespace SortingEngine.RuntimeConfiguration;

public class BaseConfiguration
{
    public int InputBufferLength { get; set; }
    public string? TemporaryFolder { get; set; }
    public int MergeBufferLength { get; set; }
    public int OutputBufferLength { get; set; }
    public int RecordsBufferLength { get; set; }
    public int? ReadStreamBufferSize { get; set; }
    public int? WriteStreamBufferSize { get; set; }
    public int SortingPhaseConcurrency { get; set; }
    public int AvailableMemory { get; set; }
    public bool? KeepReadStreamOpen { get; set; }
}