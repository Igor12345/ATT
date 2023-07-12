using Infrastructure.Parameters;
using System.Text;

namespace SortingEngine.RuntimeConfiguration;

public class BaseConfiguration
{
   public int InputBufferSize { get; set; }
   public string TemporaryFolder { get; set; }
   public int MergeBufferSize { get; set; }
   public int OutputBufferSize { get; set; }
   public int RecordsBufferSize { get; set; }
   public int AvailableMemory { get; set; }
}

public class InputParameters
{
   public string Encoding { get; set; } = "UTF16";
   public Encoding EncodingValue { get; set; }

   public string? FileName { get; set; }
}

public class ValidatedInputParameters
{
   private ValidatedInputParameters()
   {
   }

   public ValidatedInputParameters(string file, Encoding encoding)
   {
      //todo check values
      File = file;
      Encoding = encoding;
   }

   public Encoding Encoding { get; init; }

   public string File { get; init; }

   private static readonly ValidatedInputParameters _empty = new ValidatedInputParameters()
      { File = "", Encoding = Encoding.Default };
   public static ValidatedInputParameters Empty => _empty;
}

public class RuntimeConfig : IConfig
{
   private RuntimeConfig()
   {
      TemporaryFolder = "";
   }

   public int InputBufferSize { get; private set; }
   public string TemporaryFolder { get; private set; }
   public int MergeBufferSize { get; private set; }
   public int OutputBufferSize { get; private set; }
   public Encoding Encoding { get; private set; } = Encoding.UTF8;
   public int RecordsBufferSize { get; private set; }

   public static IConfig Create(Action<IConfigBuilder> buildConfig)
   {
      RuntimeConfig config = new RuntimeConfig();
      buildConfig(new ConfigBuilder(config));
      return config;
   }

   private class ConfigBuilder : IConfigBuilder
   {
      private readonly RuntimeConfig _config;

      public ConfigBuilder(RuntimeConfig config)
      {
         _config = config;
      }

      public IConfigBuilder UseInputBuffer(int inputBufferSize)
      {
         _config.InputBufferSize = inputBufferSize;
         return this;
      }

      public IConfigBuilder UseFolder(string sourceFile, string folderForChunks)
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

         _config.TemporaryFolder = folderForChunks;
         return this;
      }

      public IConfigBuilder UseMergeBuffer(int mergeBuffer)
      {
         _config.MergeBufferSize = mergeBuffer;
         return this;
      }

      public IConfigBuilder UseOutputBuffer(int outputBuffer)
      {
         _config.OutputBufferSize = outputBuffer;
         return this;
      }

      public IConfigBuilder UseRecordsBuffer(int recordsBuffer)
      {
         _config.RecordsBufferSize = recordsBuffer;
         return this;
      }

      public IConfigBuilder UseEncoding(Encoding encoding)
      {
         _config.Encoding = encoding;
         return this;
      }
   }
}