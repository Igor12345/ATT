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
         //todo implement 
         int cpus = Environment.ProcessorCount;
         long availableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
         int inputBufferLength =
            SetBufferLength(_baseConfiguration.InputBufferLength, 1024 * 1024 * 512); // 1024 * 1024 * 512 = 536870912
         int mergeBufferLength = SetBufferLength(_baseConfiguration.MergeBufferLength, 1024 * 1024); //1024 * 1024;
         int recordsLength = SetBufferLength(_baseConfiguration.RecordsBufferLength, 2_000);
         int outputBufferLength = SetBufferLength(_baseConfiguration.OutputBufferLength, 1_000);

         int readStreamBufferSize = _baseConfiguration.ReadStreamBufferSize ?? 4096;
         int writeStreamBufferSize = _baseConfiguration.WriteStreamBufferSize ?? 4096;
         string outputPath = GetOutputPath(path);

         bool useOneStepSorting = CheckForOneStep(path, inputBufferLength);
         int oneCharacterLength = encoding.GetBytes("1").Length;
         byte[] eolBytes = encoding.GetBytes(Environment.NewLine);
         int maxLineLength = (_baseConfiguration.MaxLineLength ?? 1024) * oneCharacterLength + eolBytes.Length;
         
         byte[] delimiterBytes = encoding.GetBytes((_baseConfiguration.Delimiter ?? ". "));

         //can be implemented more elegantly and concisely with using reflection or dynamic
         IConfig config = RuntimeConfiguration.RuntimeConfiguration.Create(conf => conf
            .UseInputBuffer(inputBufferLength)
            .UseMergeBuffer(mergeBufferLength)
            .UseRecordsBuffer(recordsLength)
            .UseReadStreamBufferSize(readStreamBufferSize)
            .UseWriteStreamBufferSize(writeStreamBufferSize)
            .SortingPhaseConcurrency(_baseConfiguration.SortingPhaseConcurrency)
            .UseOutputBuffer(outputBufferLength)
            .UseOutputPath(outputPath)
            .UseFileAndFolder(path, "")
            .UseKeepReadStreamOpen(_baseConfiguration.KeepReadStreamOpen ?? true)
            .UseMaxLineLength(maxLineLength)
            .UseEolBytes(eolBytes)
            .UseDelimiter(delimiterBytes)
            .UseOneWay(useOneStepSorting));

         return config;
      }

      private bool CheckForOneStep(string filePath, int inputBufferLength)
      {
         FileInfo fileInfo = new FileInfo(filePath);
         long size = fileInfo.Length;
         return size < inputBufferLength;
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
         
         //todo implement custom?
         string resultFile = $"{fileName}_sorted{delimiter}{extension}";
         
         string? directory = Path.GetDirectoryName(path);
         string pathToResult = Path.Combine(directory ?? throw new InvalidOperationException("Wrong directory."), resultFile);
         return pathToResult;
      }
   }
}
