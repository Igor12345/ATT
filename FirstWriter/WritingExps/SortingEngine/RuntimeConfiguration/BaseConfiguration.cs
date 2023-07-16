namespace SortingEngine.RuntimeConfiguration;

public class BaseConfiguration
{
    public int InputBufferLength { get; set; }
    public string TemporaryFolder { get; set; }
    public int MergeBufferLength { get; set; }
    public int OutputBufferLength { get; set; }
    public int RecordsBufferLength { get; set; }
    public int SortingPhaseConcurrency { get; set; }
    public int AvailableMemory { get; set; }
}