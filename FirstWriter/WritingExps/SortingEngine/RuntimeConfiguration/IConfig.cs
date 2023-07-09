namespace SortingEngine.RuntimeConfiguration
{
   internal interface IConfig
   {
      int InputBufferSize { get; }
      string TemporaryFolder { get; }

      static abstract IConfig Create(Action<IConfigBuilder> buildConfig) ;
   }

   internal interface IConfigBuilder
   {
      IConfigBuilder UseInputBuffer(int inputBufferSize);
      IConfigBuilder UseFolder(string folderForChunks);
   }

   internal class RuntimeConfig : IConfig
   {
      private RuntimeConfig()
      {
         
      }
      public int InputBufferSize { get; private set; }
      public string TemporaryFolder { get; private set; }
      public static IConfig Create(Action<IConfigBuilder> buildConfig)
      {
         RuntimeConfig config = new RuntimeConfig();
         buildConfig(new ConfigBuilder(config));
         return config;
      }

      private class ConfigBuilder:IConfigBuilder
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

         public IConfigBuilder UseFolder(string folderForChunks)
         {
            _config.TemporaryFolder = folderForChunks;
            return this;
         }
      }
   }
}
