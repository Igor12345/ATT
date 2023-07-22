using System.Threading.Channels;
using Moq;
using SortingEngine;
using SortingEngine.RowData;
using Xunit.Abstractions;

namespace SortingEngineTests.RowData;

public class SortingPhasePoolTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SortingPhasePoolTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task PoolManager_ShouldProvideBufferNecessaryLength()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        ReadingResult result = ReadingResult.Ok(23, 23);
        Mock<IBytesProducer> bytesProviderMock = new Mock<IBytesProducer>();
        bytesProviderMock.Setup(p => p.ProvideBytes(It.IsAny<Memory<byte>>())).Returns(result);
        FilledBufferPackage first;
        using (SortingPhasePool pool =
               new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength,100, bytesProviderMock.Object))
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            pool.Run(cts.Token);

            first = await pool.TryAcquireNextFilledBufferAsync(-1);


            bytesProviderMock.Verify(p => p.ProvideBytes(It.IsAny<Memory<byte>>()), Times.Exactly(2));
            Assert.NotNull(first);
            Assert.Equal(result.Size, first.WrittenBytesLength);
        }
    }

    [Fact]
    public async Task PoolManager_ShouldProvideOnlyAllowedNumberOfBuffers()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        ReadingResult result = ReadingResult.Ok(23, 23);
        Mock<IBytesProducer> bytesProviderMock = new Mock<IBytesProducer>();
        
        //TODO  !!! try with ProvideBytesAsync to handle the case with last, empty package
        bytesProviderMock.Setup(p => p.ProvideBytes(It.IsAny<Memory<byte>>())).Returns(result);
        FilledBufferPackage first;
        FilledBufferPackage second;
        Task<FilledBufferPackage> askingNextPackage;
        Task winner;
        Task timeLimit;
        using (SortingPhasePool pool =
               new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength, 100, bytesProviderMock.Object))
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            pool.Run(cts.Token);

            first = await pool.TryAcquireNextFilledBufferAsync(-1);
            second = await pool.TryAcquireNextFilledBufferAsync(0);
            askingNextPackage = pool.TryAcquireNextFilledBufferAsync(1);

            timeLimit = Task.Delay(50);
            winner = await Task.WhenAny(askingNextPackage, timeLimit);


            bytesProviderMock.Verify(p => p.ProvideBytes(It.IsAny<Memory<byte>>()), Times.Exactly(2));
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Same(winner, timeLimit);
        }
    }

    [Fact]
    public async Task CheckChannels()
    {
        var channel = Channel.CreateBounded<int>(6);
        _testOutputHelper.WriteLine("Channel created");

        var writer = channel.Writer;
        var reader = channel.Reader;

        for (int i = 0; i < 5; i++)
        {
            await writer.WriteAsync(i);
        }
        _testOutputHelper.WriteLine($"Items to read after writing  {channel.Reader.Count}");
        await Task.Delay(50);
        for (int i = 0; i < 3; i++)
        {
            var v = await reader.ReadAsync();
            _testOutputHelper.WriteLine($"Read {i} v={v}");
        }
        await Task.Delay(50);
        _testOutputHelper.WriteLine($"Items to read left  {channel.Reader.Count}");
        await Task.Delay(50);
        writer.Complete();
        await Task.Delay(50);
        _testOutputHelper.WriteLine($"Channel completed, items to read {channel.Reader.Count}");
        await Task.Delay(50);
        
        while (reader.TryRead(out var item))
        {
            _testOutputHelper.WriteLine($"after closing {item}");
        }
        
        _testOutputHelper.WriteLine($"Final {reader.Count}, ");
        Assert.Equal(2,3);
    }

    [Fact]
    public async Task PoolManager_ShouldAllowReuseBuffers()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;

        ReadingResult result = ReadingResult.Ok(23, 23);
        Mock<IBytesProducer> bytesProviderMock = new Mock<IBytesProducer>();
        bytesProviderMock.Setup(p => p.ProvideBytes(It.IsAny<Memory<byte>>())).Returns(result);
        FilledBufferPackage third;
        using (SortingPhasePool pool =
               new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength, 100, bytesProviderMock.Object))
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            pool.Run(cts.Token);

            FilledBufferPackage first = await pool.TryAcquireNextFilledBufferAsync(-1);
            FilledBufferPackage second = await pool.TryAcquireNextFilledBufferAsync(0);
            Task<FilledBufferPackage> askingNextPackage = pool.TryAcquireNextFilledBufferAsync(1);
            Task timeLimit = Task.Delay(50);
            Task winner = await Task.WhenAny(askingNextPackage, timeLimit);

            Assert.NotNull(first);
            Assert.NotNull(second);
            bytesProviderMock.Verify(p => p.ProvideBytes(It.IsAny<Memory<byte>>()), Times.Exactly(2));

            Assert.Same(winner, timeLimit);
            pool.ReuseBuffer(new byte[5]);

            third = await askingNextPackage;

            bytesProviderMock.Verify(p => p.ProvideBytes(It.IsAny<Memory<byte>>()), Times.Exactly(3));
            Assert.Equal(result.Size, third.WrittenBytesLength);
        }
    }
}