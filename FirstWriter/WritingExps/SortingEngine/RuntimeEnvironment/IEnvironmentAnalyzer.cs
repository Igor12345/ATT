﻿using System.Text;
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
         //todo
         int cpus = Environment.ProcessorCount;
         var availableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
         int inputBufferLength =
            SetBufferLength(_baseConfiguration.InputBufferLength, 1024 * 1024 * 512); // 1024 * 1024 * 512 = 536870912
         int mergeBufferLength = SetBufferLength(_baseConfiguration.MergeBufferLength, 1024 * 1024); //1024 * 1024;
         int recordsLength = SetBufferLength(_baseConfiguration.RecordsBufferLength, 2_000);
         int outputBufferLength = SetBufferLength(_baseConfiguration.OutputBufferLength, 1_000);

         int readStreamBufferSize = _baseConfiguration.ReadStreamBufferSize ?? 4096;
         int writeStreamBufferSize = _baseConfiguration.WriteStreamBufferSize ?? 4096;
         string outputPath = GetOutputPath(path);

         bool useOneStepSorting = CheckForOneStep(path, inputBufferLength);
         
         //can be implemented more elegantly and concisely with using reflection or dynamic
         var config = RuntimeConfiguration.RuntimeConfiguration.Create(conf => conf
            .UseInputBuffer(inputBufferLength)
            .UseMergeBuffer(mergeBufferLength)
            .UseRecordsBuffer(recordsLength)
            .UseReadStreamBufferSize(readStreamBufferSize)
            .UseWriteStreamBufferSize(writeStreamBufferSize)
            .SortingPhaseConcurrency(_baseConfiguration.SortingPhaseConcurrency)
            .UseOutputBuffer(outputBufferLength)
            .UseOutputPath(outputPath)
            .UseFileAndFolder(path, "")
            //todo merge with the preset config
            .UseEncoding(encoding)
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
         
         //todo custom?
         string resultFile = $"{fileName}_sorted{delimiter}{extension}";
         //todo
         string? directory = Path.GetDirectoryName(path);
         string pathToResult = Path.Combine(directory, resultFile);
         return pathToResult;
      }
   }
}
