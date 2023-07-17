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
      string Output { get; }
      string TemporaryFolder { get; }
      string InputFile { get; }
      Encoding Encoding { get; }
      bool UseOneWay { get; }

      // static abstract IConfig Create(Action<IConfigBuilder> buildConfig);
      
   }
}
