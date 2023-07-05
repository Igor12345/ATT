using System.Diagnostics;

namespace SimpleWriter;

internal class InfoRecord
{
   private readonly string _fileName;
   private readonly string _additionalInfo;
   private Stopwatch _stopwatch;

   public DateTime StartTime { get; }

   private InfoRecord(string fileName, string additionalInfo)
   {
      _fileName = fileName;
      _additionalInfo = additionalInfo;
      StartTime = DateTime.UtcNow;
   }

   public static InfoRecord Start(string fileName, string additionalInfo)
   {
      InfoRecord record = new InfoRecord(fileName, additionalInfo)
      {
         _stopwatch = Stopwatch.StartNew()
      };
      return record;
   }

   public void Stop()
   {
      _stopwatch.Stop();
   }

   public string PrintResult()
   {
      if (_stopwatch.IsRunning)
      {
         return $"File {_fileName} is still processed.";
      }

      return
         $"File {_fileName} ({_additionalInfo}) was processed for {_stopwatch.Elapsed.Seconds} sec and {_stopwatch.ElapsedMilliseconds} milliseconds.";
   }
}