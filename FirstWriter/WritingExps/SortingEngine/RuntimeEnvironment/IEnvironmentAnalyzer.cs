using System.Text;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.RuntimeEnvironment
{
   public interface IEnvironmentAnalyzer
   {
      IConfig SuggestConfig(string path, Encoding encoding);
   }

   public class EnvironmentAnalyzer : IEnvironmentAnalyzer
   {
      public IConfig SuggestConfig(string path, Encoding encoding)
      {
         int inputBufferSize = Int32.MaxValue-1000;
         int mergeBuffer = 1024 * 1024;

         var config = RuntimeConfig.Create(conf => conf
            .UseInputBuffer(inputBufferSize)
            .UseMergeBuffer(mergeBuffer)
            .UseFolder(path, "")
            //todo merge with the preset config
            .UseEncoding(encoding));
         return config;
      }
   }
}
