using System.Text;
using InfoStructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing
{
   public class RecordsRetriever
   {
      private readonly Func<Stream> _streamFactory;
      private readonly Stream _inputStream;
      private readonly Encoding _encoding;

      public RecordsRetriever(Func<Stream> streamFactory)
      {
         _streamFactory = Guard.NotNull(streamFactory, nameof(streamFactory));
         _encoding = Encoding.UTF8;
      }

      public RecordsRetriever(Func<Stream> streamFactory, Encoding encoding)
      {
         _streamFactory = Guard.NotNull(streamFactory, nameof(streamFactory));
         _encoding = Guard.NotNull(encoding, nameof(encoding));
      }

      public RecordsRetriever(Stream inputStream)
      {
         _inputStream = Guard.NotNull(inputStream, nameof(inputStream));
         _encoding = Encoding.UTF8;
      }

      public RecordsRetriever(Stream inputStream, Encoding encoding)
      {
         _inputStream = Guard.NotNull(inputStream, nameof(inputStream));
         _encoding = Guard.NotNull(encoding, nameof(encoding));
      }

      public async Task<ReadingResult> ReadChunkAsync(byte[] buffer, CancellationToken cancellationToken)
      {
         int length = await _inputStream.ReadAsync(buffer, 0,buffer.Length, cancellationToken);
         return new ReadingResult() { Success = true, Size = length };
      }
   }
}
