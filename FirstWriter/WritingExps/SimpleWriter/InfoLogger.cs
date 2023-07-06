namespace SimpleWriter;

internal class InfoLogger : IAsyncDisposable, IDisposable
{
   private readonly string _fileName;
   readonly InfoRecord _record;
   public InfoLogger(string fileName, string additionalInfo)
   {
      _fileName = fileName;
      _record = InfoRecord.Start(fileName, additionalInfo);
   }

   public void Dispose()
   {
      _record.Stop();
      string fileName = @$"d://temp/ATT/{_fileName}.log";
      using var stream = File.CreateText(fileName);
      stream.WriteLine(_record.PrintResult());
   }

   public async ValueTask DisposeAsync()
   {
      _record.Stop();
      string fileName = @$"d://temp/ATT/{_fileName}.log";
      await using var stream = File.CreateText(fileName);
      await stream.WriteLineAsync(_record.PrintResult());
   }
}