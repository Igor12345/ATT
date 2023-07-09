using System.Text;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SimpleReader;

public class InputProcessor
{
   public async Task<Result> ReadRecords(string path, byte[] buffer, LineRecord[] records)
   {
      var file = File.OpenHandle(path);
      await using FileStream stream = File.OpenRead(path);
      RecordsRetriever retriever = new RecordsRetriever(stream);
      var readingResult = await retriever.ReadChunk(buffer, 0, buffer.Length);
      if (!readingResult.Success)
         return new Result(false, readingResult.Message);

      var encoding = Encoding.UTF8;
      RecordsExtractor extractor =
         new RecordsExtractor(encoding.GetBytes(Environment.NewLine), encoding.GetBytes(". "));
      return extractor.SplitOnRecords(buffer, records);
   }

   public async Task<ExtractionResult> ReadMemoryRecords(string path, byte[] buffer, LineMemory[] records)
   {
      await using FileStream stream = File.OpenRead(path);
      RecordsRetriever retriever = new RecordsRetriever(stream);
      var readingResult = await retriever.ReadChunk(buffer, 0, buffer.Length);
      if (!readingResult.Success)
         return ExtractionResult.Error(readingResult.Message);

      var encoding = Encoding.UTF8;
      RecordsExtractor extractor =
         new RecordsExtractor(encoding.GetBytes(Environment.NewLine), encoding.GetBytes(". "));
      return extractor.SplitOnMemoryRecords(buffer, records);
   }
}