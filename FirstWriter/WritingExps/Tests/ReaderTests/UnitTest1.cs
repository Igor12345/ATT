using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using SimpleReader;
using SortingEngine;
using SortingEngine.Entities;
using SortingEngine.Sorters;

namespace ReaderTests
{
   public class UnitTest1
   {
      [Fact]
      public async Task CanRead()
      {
         InputProcessor inputProcessor = new InputProcessor();
         string fileName = "fourth";
         string path = @$"d://temp/ATT/{fileName}.txt";
         byte[] bytes = new byte[100_000];
         LineRecord[] records = new LineRecord[10_000];
         var result = await inputProcessor.ReadRecords(path, bytes, records);

         var first = records[0];

         Assert.True(result.Success);
      }

      [Fact]
      public async Task CanSort()
      {
         byte[] bytes = new byte[2_000_000_000];
         LineRecord[] records = new LineRecord[100000];

         InputProcessor inputProcessor = new InputProcessor();
         string fileName = "fourth";
         string path = @$"d://temp/ATT/{fileName}.txt";
         Result result = await inputProcessor.ReadRecords(path, bytes, records);

         Stopwatch stopwatch = Stopwatch.StartNew();
         RecordsSorter sorter = new RecordsSorter();
         var sorted = sorter.Sort(records);

         stopwatch.Stop();

         LineAsString[] originalRecords = ConvertToStrings(records);
         LineAsString[] sortedRecords = ConvertToStrings(sorted);

         var min = stopwatch.Elapsed.Minutes;
         var sec = stopwatch.Elapsed.Seconds;
         var total = stopwatch.Elapsed.TotalMilliseconds;

         Assert.Equal(sorted.Length, records.Length);
      }

      [Fact]
      public async Task CanSortOnSite()
      {
         byte[] bytes = new byte[2_000_000_000];
         LineMemory[] records = new LineMemory[100000];

         InputProcessor inputProcessor = new InputProcessor();
         string fileName = "fourth";
         string path = @$"d://temp/ATT/{fileName}.txt";
         var result = await inputProcessor.ReadMemoryRecords(path, bytes, records);

         Stopwatch stopwatch = Stopwatch.StartNew();
         InSiteRecordsSorter sorter = new InSiteRecordsSorter(bytes);
         var sorted = sorter.Sort(records);

         stopwatch.Stop();

         LineAsString[] originalRecords = ConvertToStrings(records, bytes);
         LineAsString[] sortedRecords = ConvertToStrings(sorted, bytes);

         var min = stopwatch.Elapsed.Minutes;
         var sec = stopwatch.Elapsed.Seconds;
         var total = stopwatch.Elapsed.TotalMilliseconds;

         Assert.Equal(sorted.Length, records.Length);
      }

      private LineAsString[] ConvertToStrings(LineMemory[] lineRecords, byte[] source)
      {
         Encoding encoding = Encoding.UTF8;

         LineAsString[] result = new LineAsString[lineRecords.Length];
         for (int i = 0; i < lineRecords.Length; i++)
         {
            result[i] = new LineAsString(lineRecords[i].Number,
               encoding.GetString(source[lineRecords[i].From..lineRecords[i].To]));
         }
         return result;

      }

      private LineAsString[] ConvertToStrings(LineRecord[] lineRecords)
      {
         Encoding encoding = Encoding.UTF8;

         LineAsString[] result = new LineAsString[lineRecords.Length];
         for (int i = 0; i < lineRecords.Length; i++)
         {
            result[i] = new LineAsString(lineRecords[i].Number, encoding.GetString(lineRecords[i].Text));
         }
         return result;
      }

      [Fact]
      public void StringTest()
      {
         int i=12;
         string result = i.ToString("D4");
         string d = result;

         string fileName = @"d://temp/ATT/second.txt";
         string path1 = Path.GetDirectoryName(fileName);
         var file= Path.GetFileNameWithoutExtension(fileName);
         string dirName = $"{file}_{Guid.NewGuid()}";
         ReadOnlySpan<char> path2 = Path.GetDirectoryName(@"d://temp/ATT/".AsSpan());
         var path3 = Path.GetDirectoryName(@"d://temp/ATT".AsSpan());
         var path4 = Path.GetDirectoryName(@"d://temp/ATT/second".AsSpan());

      }

      [Fact]
      public void MultipleReturn()
      {
         var buffer = ArrayPool<int>.Shared.Rent(5);
         for (int i = 0; i < 5; i++)
         {
            buffer[i] = i;
         }
         var result = buffer.ToArray();

         ArrayPool<int>.Shared.Return(buffer);
         ArrayPool<int>.Shared.Return(buffer);
         ArrayPool<int>.Shared.Return(buffer);

         Assert.Equal(result[4], 4);
      }
   }
}