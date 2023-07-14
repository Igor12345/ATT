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
      private readonly BaseConfiguration _baseConfiguration;

      public EnvironmentAnalyzer(BaseConfiguration baseConfiguration)
      {
         _baseConfiguration = Guard.NotNull(baseConfiguration);
      }

      public IConfig SuggestConfig(string path, Encoding encoding)
      {
         int inputBufferLength =
            SetBufferLength(_baseConfiguration.InputBufferLength, 1024 * 1024 * 512); // 1024 * 1024 * 512 = 536870912
         int mergeBufferLength = SetBufferLength(_baseConfiguration.MergeBufferLength, 1024 * 1024); //1024 * 1024;
         int recordsLength = SetBufferLength(_baseConfiguration.RecordsBufferLength, 2_000);
         int outputBufferLength = SetBufferLength(_baseConfiguration.OutputBufferLength, 1_000);

         string outputPath = GetOutputPath(path);
         var config = RuntimeConfig.Create(conf => conf
            .UseInputBuffer(inputBufferLength)
            .UseMergeBuffer(mergeBufferLength)
            .UseRecordsBuffer(recordsLength)
            .UseOutputBuffer(outputBufferLength)
            .UseOutputPath(outputPath)
            .UseFolder(path, "")
            //todo merge with the preset config
            .UseEncoding(encoding));
         return config;
      }

      private int SetBufferLength(int configurationLength, int defaultLength)
      {
         int bufferLength = configurationLength > 0 ? configurationLength : defaultLength;
         return Math.Min(bufferLength, Array.MaxLength);
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
         
         //todo custom?
         string resultFile = $"{fileName}_sorted{delimiter}{extension}";
         //todo
         string? directory = Path.GetDirectoryName(path);
         string pathToResult = Path.Combine(directory, resultFile);
         return pathToResult;
      }
   }
}
