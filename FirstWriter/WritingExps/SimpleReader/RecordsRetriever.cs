using System.Text;
using SortingEngine;

namespace SimpleReader
{
   public class RecordsRetriever
   {
      private readonly Func<Stream> _streamFactory;
      private readonly Stream _inputStream;
      private readonly Encoding _encoding;

      public RecordsRetriever(Func<Stream> streamFactory, Encoding? encoding = null)
      {
         _streamFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
         _encoding = encoding ?? Encoding.UTF8;
      }

      public RecordsRetriever(Stream inputStream, Encoding? encoding = null)
      {
         _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
         _encoding = encoding ?? Encoding.UTF8;
      }

      public async Task<ReadingResult> ReadChunk(byte[] buffer, int offset, int count)
      {
         // using BinaryReader reader = new BinaryReader(_inputStream, _encoding, true);
         // reader.BaseStream.Position = offset;
         int length = await _inputStream.ReadAsync(buffer, offset, count);
         return new ReadingResult() { Success = true, Size = length };
      }
   }
}