using System.Text;
using Infrastructure.Parameters;
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

         string outputPath = GetOutputPath(path);
         var config = RuntimeConfig.Create(conf => conf
            .UseInputBuffer(inputBufferSize)
            .UseMergeBuffer(mergeBuffer)
            .UseRecordsBuffer(2_000_000)
            .UseOutputBuffer(1_000)
            .UseOutputPath(outputPath)
            .UseFolder(path, "")
            //todo merge with the preset config
            .UseEncoding(encoding));
         return config;
      }

      public IConfig SuggestConfig(ValidatedInputParameters inputParameters)
      {
         return SuggestConfig(inputParameters.File, inputParameters.Encoding);
      }

      private string GetOutputPath(string path)
      {
         path = Guard.FileExist(path);
         var fileName = Path.GetFileNameWithoutExtension(path);
         var extension = Path.GetExtension(path);
         string delimiter = extension.Length > 0 ? "." : "";
         string resultFile = $"{fileName}_sorted{delimiter}{extension}";
         //todo
         string? directory = Path.GetDirectoryName(path);
         string pathToResult = Path.Combine(directory, resultFile);
         return pathToResult;
      }
   }
}
