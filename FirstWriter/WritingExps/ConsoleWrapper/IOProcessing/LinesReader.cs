using System.Text;
using Infrastructure.Parameters;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing
{
   public class LinesReader 
   {
      private readonly Func<Stream> _streamFactory;
      private readonly Stream _inputStream;
      private readonly Encoding _encoding;

      public LinesReader(Func<Stream> streamFactory)
      {
         _streamFactory = Guard.NotNull(streamFactory);
         _encoding = Encoding.UTF8;
      }

      public LinesReader(Func<Stream> streamFactory, Encoding encoding)
      {
         _streamFactory = Guard.NotNull(streamFactory);
         _encoding = Guard.NotNull(encoding);
      }

      public LinesReader(Stream inputStream)
      {
         _inputStream = Guard.NotNull(inputStream);
         _encoding = Encoding.UTF8;
      }

      public LinesReader(Stream inputStream, Encoding encoding)
      {
         _inputStream = Guard.NotNull(inputStream);
         _encoding = Guard.NotNull(encoding);
      }

      //todo memory
      public async Task<ReadingResult> ReadChunkAsync(byte[] buffer, int offset, CancellationToken cancellationToken)
      {
         int length = await _inputStream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
         return new ReadingResult() { Success = true, Size = length + offset };
      }

      //todo memory
      public  ReadingResult ReadChunk(byte[] buffer, int offset)
      {
         int length = _inputStream.Read(buffer, offset, buffer.Length - offset);
         return new ReadingResult() { Success = true, Size = length + offset };
      }
   }
}
