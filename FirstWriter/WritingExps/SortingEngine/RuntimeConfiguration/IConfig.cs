namespace SortingEngine.RuntimeConfiguration
{
   internal interface IConfig
   {
      int InputBufferSize { get; }
      string TemporaryFolder { get; }
      int MergeBufferSize { get; }
      int OutputBufferSize { get; }

      static abstract IConfig Create(Action<IConfigBuilder> buildConfig);
   }
}
