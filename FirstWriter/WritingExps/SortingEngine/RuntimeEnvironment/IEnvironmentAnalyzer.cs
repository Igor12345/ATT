﻿using System.Text;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.RuntimeEnvironment
{
   public interface IEnvironmentAnalyzer
   {
      IConfig SuggestConfig(string path, Encoding encoding);
      IConfig SuggestConfig(ValidatedInputParameters inputParameters);
   }

   public class EnvironmentAnalyzer : IEnvironmentAnalyzer
   {
      public IConfig SuggestConfig(string path, Encoding encoding)
      {
         int inputBufferSize = 1024 * 1024 * 512;
         int mergeBuffer = 1024 * 1024;

         var config = RuntimeConfig.Create(conf => conf
            .UseInputBuffer(inputBufferSize)
            .UseMergeBuffer(mergeBuffer)
            .UseRecordsBuffer(2_000_000)
            .UseOutputBuffer(1_000)
            .UseFolder(path, "")
            //todo merge with the preset config
            .UseEncoding(encoding));
         return config;
      }

      public IConfig SuggestConfig(ValidatedInputParameters inputParameters)
      {
         return SuggestConfig(inputParameters.File, inputParameters.Encoding);
      }
   }
}
