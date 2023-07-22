using Moq;
using SortingEngine;
using SortingEngine.RowData;

namespace SortingEngineTests.RowData;

public class SortingPhasePoolTests
{
    [Fact]
    public async Task PoolManager_ShouldProvideBufferNecessaryLength()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        ReadingResult result = ReadingResult.Ok(23,23);
        Mock<IBytesProducer> bytesProviderMock = new Mock<IBytesProducer>();
        bytesProviderMock.Setup(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(result);
        FilledBufferPackage first;
        using (SortingPhasePool pool =
               new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength, bytesProviderMock.Object))
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            pool.Run(cts.Token);

            first = await pool.TryAcquireNextFilledBufferAsync(-1);
        }

        bytesProviderMock.Verify(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>()), Times.Exactly(2));
        Assert.NotNull(first);
        Assert.Equal(result.Size, first.WrittenBytes);
    }

    [Fact]
    public async Task PoolManager_ShouldProvideOnlyAllowedNumberOfBuffers()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        ReadingResult result = ReadingResult.Ok(23,23);
        Mock<IBytesProducer> bytesProviderMock = new Mock<IBytesProducer>();
        bytesProviderMock.Setup(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(result);
        FilledBufferPackage first;
        FilledBufferPackage second;
        Task<FilledBufferPackage> askingNextPackage;
        using (SortingPhasePool pool =
               new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength, bytesProviderMock.Object))
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            pool.Run(cts.Token);

            first = await pool.TryAcquireNextFilledBufferAsync(-1);
            second = await pool.TryAcquireNextFilledBufferAsync(0);
            askingNextPackage = pool.TryAcquireNextFilledBufferAsync(1);
        }
        Task timeLimit = Task.Delay(50);

        Task winner = await Task.WhenAny(askingNextPackage, timeLimit);
        
        bytesProviderMock.Verify(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>()), Times.Exactly(2));
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Same(winner, timeLimit);
    }

    [Fact]
    public async Task PoolManager_ShouldAllowReuseBuffers()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        
        ReadingResult result = ReadingResult.Ok(23,23);
        Mock<IBytesProducer> bytesProviderMock = new Mock<IBytesProducer>();
        bytesProviderMock.Setup(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(result);
        FilledBufferPackage first;
        FilledBufferPackage second;
        Task<FilledBufferPackage> askingNextPackage;
        using (SortingPhasePool pool =
               new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength, bytesProviderMock.Object))
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            pool.Run(cts.Token);

            first = await pool.TryAcquireNextFilledBufferAsync(-1);
            second = await pool.TryAcquireNextFilledBufferAsync(0);
            askingNextPackage = pool.TryAcquireNextFilledBufferAsync(1);
            Task timeLimit = Task.Delay(50);
            Task winner = await Task.WhenAny(askingNextPackage, timeLimit);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Same(winner, timeLimit);
            bytesProviderMock.Verify(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>()), Times.Exactly(2));

            pool.ReuseBuffer(new byte[5]);
        }

        FilledBufferPackage third = await askingNextPackage;
        bytesProviderMock.Verify(p => p.ProvideBytesAsync(It.IsAny<Memory<byte>>()), Times.Exactly(3));
        Assert.Equal(result.Size, third.WrittenBytes);
    }
}