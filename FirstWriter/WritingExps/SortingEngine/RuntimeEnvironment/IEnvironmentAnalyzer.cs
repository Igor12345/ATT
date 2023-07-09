using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.RuntimeEnvironment
{
   internal interface IEnvironmentAnalyzer
   {
      IConfig SuggestConfig();
   }

   internal class EnvironmentAnalyzer : IEnvironmentAnalyzer
   {
      public IConfig SuggestConfig()
      {
         int inputBufferSize = Int32.MaxValue-1000;
         int mergeBuffer = 1024 * 1024;

         var config = RuntimeConfig.Create(conf => conf
            .UseInputBuffer(inputBufferSize)
            .UseMergeBuffer(mergeBuffer)
            .UseFolder(""));
         return config;
      }
   }
}
