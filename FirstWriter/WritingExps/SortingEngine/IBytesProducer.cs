using OneOf;
using OneOf.Types;
using SortingEngine.RowData;

namespace SortingEngine;

public interface IBytesProducer : IAsyncDisposable
{
    // Task<OneOf<Result<int>, Error<string>>> PopulateAsyncFunc(byte[] buffer);
    Task<ReadingResult> ReadBytesAsync(byte[] buffer, int offset, CancellationToken cancellationToken);
    ReadingResult ReadBytes(byte[] buffer, int offset);
    Task<ReadingPhasePackage> ProcessPackageAsync(ReadingPhasePackage inputPackage);
}