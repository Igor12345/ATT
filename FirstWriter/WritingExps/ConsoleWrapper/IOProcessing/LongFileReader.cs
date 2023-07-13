﻿using System.Text;
using Infrastructure.Parameters;
using OneOf;
using OneOf.Types;
using SortingEngine;

namespace ConsoleWrapper.IOProcessing;

//todo rename
internal class LongFileReader : IBytesProducer, IAsyncDisposable
{
   private readonly string _fullFileName;
   private readonly Encoding _encoding;
   private FileStream _stream;
   private long _lastPosition = 0;

   public LongFileReader(string fullFileName, Encoding encoding)
   {
      _fullFileName = Guard.FileExist(fullFileName);
      _encoding = Guard.NotNull(encoding);
   }

   public Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer)
   {
      throw new NotImplementedException();
   }

   public async Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset,
      CancellationToken cancellationToken)
   {
      await using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      await using RecordsReader reader = new RecordsReader(stream);
      var readingResult = await reader.ReadChunkAsync(buffer, offset, cancellationToken);
      if (!readingResult.Success)
         return readingResult;

      _lastPosition += readingResult.Size;
      return readingResult;
   }

   public ReadingResult ReadBytes(byte[] buffer, int offset)
   {
      using FileStream stream = File.OpenRead(_fullFileName);
      if (_lastPosition > 0)
         stream.Seek(_lastPosition, SeekOrigin.Begin);

      using RecordsReader reader = new RecordsReader(stream);
      var readingResult = reader.ReadChunk(buffer, offset);
      if (!readingResult.Success)
         return readingResult;

      _lastPosition += readingResult.Size;
      return readingResult;
   }

   public  ValueTask DisposeAsync()
   {
      return _stream?.DisposeAsync() ?? ValueTask.CompletedTask;
   }
}