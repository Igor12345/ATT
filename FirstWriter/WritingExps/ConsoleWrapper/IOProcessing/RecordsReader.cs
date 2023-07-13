using System.Text;
using Infrastructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing
{
   public class RecordsReader : IAsyncDisposable
   {
      private readonly Func<Stream> _streamFactory;
      private readonly Stream _inputStream;
      private readonly Encoding _encoding;

      public RecordsReader(Func<Stream> streamFactory)
      {
         _streamFactory = Guard.NotNull(streamFactory);
         _encoding = Encoding.UTF8;
      }

      public RecordsReader(Func<Stream> streamFactory, Encoding encoding)
      {
         _streamFactory = Guard.NotNull(streamFactory);
         _encoding = Guard.NotNull(encoding);
      }

      public RecordsReader(Stream inputStream)
      {
         _inputStream = Guard.NotNull(inputStream);
         _encoding = Encoding.UTF8;
      }

      public RecordsReader(Stream inputStream, Encoding encoding)
      {
         _inputStream = Guard.NotNull(inputStream);
         _encoding = Guard.NotNull(encoding);
      }

      //todo memory
      public async Task<ReadingResult> ReadChunkAsync(byte[] buffer, int offset, CancellationToken cancellationToken)
      {
         int length = await _inputStream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
         buffer = null;
         return new ReadingResult() { Success = true, Size = length + offset };
      }

      public async ValueTask DisposeAsync()
      {
         await _inputStream.DisposeAsync();
      }
   }
}
