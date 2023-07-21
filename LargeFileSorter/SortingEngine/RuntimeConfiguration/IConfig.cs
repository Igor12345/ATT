using System.Text;

namespace SortingEngine.RuntimeConfiguration
{
   public interface IConfig
   {
      int SortingPhaseConcurrency { get; }
      int InputBufferLength { get; }
      int MergeBufferLength { get; }
      int OutputBufferLength { get; }
      int RecordsBufferLength { get; }
      int ReadStreamBufferSize { get; }
      int WriteStreamBufferSize { get; }
      string Output { get; }
      string TemporaryFolder { get; }
      string InputFile { get; }
      // Encoding Encoding { get; }
      int MaxLineLength { get; }
      //todo freeze
      byte[] DelimiterBytes { get; }
      byte[] EolBytes { get; }
      bool UseOneWay { get; }
      bool KeepReadStreamOpen { get; }
   }
}
