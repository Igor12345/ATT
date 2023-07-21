using SortingEngine.RowData;

namespace SortingEngine;

public interface IBytesProducer : IAsyncDisposable, IDisposable
{
    Task<ReadingPhasePackage> WriteBytesToBufferAsync(ReadingPhasePackage inputPackage);
    Task<ReadingResult> WriteBytesToBufferAsync(byte[] buffer);
}