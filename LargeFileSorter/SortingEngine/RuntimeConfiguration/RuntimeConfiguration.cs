using Infrastructure.Parameters;

namespace SortingEngine.RuntimeConfiguration;

public class RuntimeConfiguration : IConfig
{
   private RuntimeConfiguration()
   {
      TemporaryFolder = "";
   }

   public int SortingPhaseConcurrency { get; private set; } = 4;
   public int InputBufferLength { get; private set; }
   public string TemporaryFolder { get; private set; }
   public string InputFile { get; private set; } = null!;
   public int MergeBufferLength { get; private set; }
   public int OutputBufferLength { get; private set; }
   // public Encoding Encoding { get; private set; } = Encoding.UTF8;
   public int MaxLineLength { get; private set; }
   public byte[] DelimiterBytes { get; private set; }
   public byte[] EolBytes { get; private set; }
   public int RecordsBufferLength { get; private set; }
   public string Output { get; private set; } = null!;
   public int ReadStreamBufferSize { get; private set; }
   public int WriteStreamBufferSize { get; private set; }
   public bool UseOneWay { get; private set; }
   public bool KeepReadStreamOpen { get; private set; }

   public static IConfig Create(Action<IConfigBuilder> buildConfig)
   {
      RuntimeConfiguration configuration = new RuntimeConfiguration();
      buildConfig(new ConfigBuilder(configuration));
      return configuration;
   }

   private class ConfigBuilder : IConfigBuilder
   {
      private readonly RuntimeConfiguration _configuration;

      public ConfigBuilder(RuntimeConfiguration configuration)
      {
         _configuration = configuration;
      }

      public IConfigBuilder UseInputBuffer(int inputBufferSize)
      {
         _configuration.InputBufferLength = inputBufferSize;
         return this;
      }

      public IConfigBuilder UseFileAndFolder(string sourceFile, string folderForChunks)
      {
         if (string.IsNullOrEmpty(folderForChunks))
         {
            sourceFile = Guard.PathExist(sourceFile);

            var sourceDir = Path.GetDirectoryName(sourceFile.AsSpan());
            string fileName = "";
            if (File.Exists(sourceFile))
            {
               fileName = Path.GetFileNameWithoutExtension(sourceFile);
            }

            string dirName = $"{fileName}_{Guid.NewGuid()}";
            folderForChunks = Path.Combine(sourceDir.ToString(), dirName);
         }

         if (folderForChunks.Equals("temp", StringComparison.OrdinalIgnoreCase))
         {
            folderForChunks = Directory.CreateTempSubdirectory().FullName;
         }

         _configuration.TemporaryFolder = folderForChunks;
         _configuration.InputFile = sourceFile;
         return this;
      }

      public IConfigBuilder UseMergeBuffer(int mergeBuffer)
      {
         _configuration.MergeBufferLength = mergeBuffer;
         return this;
      }

      public IConfigBuilder UseOutputBuffer(int outputBuffer)
      {
         _configuration.OutputBufferLength = outputBuffer;
         return this;
      }

      public IConfigBuilder UseRecordsBuffer(int recordsBuffer)
      {
         _configuration.RecordsBufferLength = recordsBuffer;
         return this;
      }

      public IConfigBuilder UseReadStreamBufferSize(int readStreamBufferSize)
      {
         _configuration.ReadStreamBufferSize = readStreamBufferSize;
         return this;
      }

      public IConfigBuilder UseWriteStreamBufferSize(int writeStreamBufferSize)
      {
         _configuration.WriteStreamBufferSize = writeStreamBufferSize;
         return this;
      }

      public IConfigBuilder SortingPhaseConcurrency(int sortingPhaseConcurrency)
      {
         _configuration.SortingPhaseConcurrency = sortingPhaseConcurrency;
         return this;
      }

      public IConfigBuilder UseOutputPath(string outputPath)
      {
         _configuration.Output = outputPath;
         return this;
      }

      public IConfigBuilder UseDelimiter(byte[] delimiterBytes)
      {
         _configuration.DelimiterBytes = delimiterBytes;
         return this;
      }

      public IConfigBuilder UseEolBytes(byte[] eolBytes)
      {
         _configuration.EolBytes = eolBytes;
         return this;
      }

      public IConfigBuilder UseMaxLineLength(int maxLineLength)
      {
         _configuration.MaxLineLength = maxLineLength;
         return this;
      }

      public IConfigBuilder UseOneWay(bool useOneStepSorting)
      {
         _configuration.UseOneWay = useOneStepSorting;
         return this;
      }

      public IConfigBuilder UseKeepReadStreamOpen(bool keepReadStreamOpen)
      {
         _configuration.KeepReadStreamOpen = keepReadStreamOpen;
         return this;
      }
   }

}