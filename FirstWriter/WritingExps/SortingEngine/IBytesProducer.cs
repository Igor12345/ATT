using OneOf;
using OneOf.Types;
using SortingEngine.RowData;

namespace SortingEngine;

public interface IBytesProducer : IAsyncDisposable, IDisposable
{
    ReadingResult ReadBytes(byte[] buffer, int offset);
    Task<ReadingPhasePackage> ProcessPackageAsync(ReadingPhasePackage inputPackage);
}