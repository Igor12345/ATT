using SortingEngine.RowData;

namespace SortingEngine;

public interface IBytesProducer : IAsyncDisposable, IDisposable
{
    // Task<ReadyForExtractionPackage> WriteBytesToBufferAsync(ReadyForExtractionPackage inputPackage);
    Task<ReadingResult> ProvideBytesAsync(Memory<byte> buffer);
    ReadingResult ProvideBytes(Memory<byte> buffer);
}