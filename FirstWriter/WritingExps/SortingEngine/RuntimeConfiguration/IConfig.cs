using System.Text;

namespace SortingEngine.RuntimeConfiguration
{
   public interface IConfig
   {
      int InputBufferSize { get; }
      string TemporaryFolder { get; }
      int MergeBufferSize { get; }
      int OutputBufferSize { get; }
      Encoding Encoding { get; }
      int RecordsBufferSize { get; }

      static abstract IConfig Create(Action<IConfigBuilder> buildConfig);
   }
}
