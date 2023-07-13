using System.Text;
using Infrastructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing
{
   public class RecordsReader:IAsyncDisposable, IDisposable
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

      public async Task<ReadingResult> ReadChunkAsync(ArrayWrapper<byte> wrapper, int offset, CancellationToken cancellationToken)
      {
         int length = await _inputStream.ReadAsync(wrapper.Array, offset, wrapper.Array.Length - offset, cancellationToken);
         wrapper = null;
         return new ReadingResult() { Success = true, Size = length + offset };
      }

      public void Dispose()
      {
         _inputStream.Dispose();
      }

      public ReadingResult ReadChunk(ArrayWrapper<byte> wrapper, int offset, CancellationToken cancellationToken)
      {
         int length = _inputStream.Read(wrapper.Array, offset, wrapper.Array.Length - offset);
         wrapper = null;
         return new ReadingResult() { Success = true, Size = length + offset };
      }
   }
}
