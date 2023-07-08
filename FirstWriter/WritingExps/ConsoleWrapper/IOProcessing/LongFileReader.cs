﻿using System.IO;
using System.Text;
using InfoStructure.Parameters;
using OneOf;
using OneOf.Types;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing;

internal class LongFileReader : IBytesProducer, IDisposable
{
   private readonly string _fullFileName;
   private readonly Encoding _encoding;
   private FileStream _stream;
   private long _lastPosition = 0;

   public LongFileReader(string fullFileName, Encoding encoding)
   {
      _fullFileName = Guard.FileExist(fullFileName);
      _encoding = Guard.NotNull(encoding, nameof(encoding));
   }

    public Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public async Task<ReadingResult> PopulateAsync(byte[] buffer)
   {
      await using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      RecordsRetriever retriever = new RecordsRetriever(stream);
      var readingResult = await retriever.ReadChunk(buffer, 0, buffer.Length);
      if (!readingResult.Success)
         return readingResult;
   }

    public void Dispose()
    {
    }
}