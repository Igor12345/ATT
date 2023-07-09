using OneOf;
using OneOf.Types;

namespace SortingEngine;

public interface IBytesProducer
{
   Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer);
   Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset, CancellationToken cancellationToken);
}