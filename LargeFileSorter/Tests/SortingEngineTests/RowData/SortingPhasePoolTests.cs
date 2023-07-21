using SortingEngine.RowData;

namespace SortingEngineTests.RowData;

public class SortingPhasePoolTests
{
    [Fact]
    public async Task PoolManager_ShouldProvideBufferNecessaryLength()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        SortingPhasePool pool =
            new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength);

        ReadingPhasePackage package = await pool.TryAcquireNextAsync();

        Assert.NotNull(package);
        Assert.NotNull(package.ParsedRecords);
        Assert.NotNull(package.RowData);
        Assert.True(package.RowData.Length >= minBytesBufferLength);
    }

    [Fact]
    public async Task PoolManager_ShouldProvideOnlyAllowedNumberOfBuffers()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        SortingPhasePool pool =
            new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength);

        ReadingPhasePackage package1 = await pool.TryAcquireNextAsync();
        ReadingPhasePackage package2 = await pool.TryAcquireNextAsync();
        Task<ReadingPhasePackage> askingNextPackage = pool.TryAcquireNextAsync();
        Task timeLimit = Task.Delay(50);

        Task winner = await Task.WhenAny(askingNextPackage, timeLimit);

        Assert.NotNull(package1);
        Assert.NotNull(package2);
        Assert.Same(winner, timeLimit);
    }

    [Fact]
    public async Task PoolManager_ShouldAllowReuseBuffers()
    {
        int minBytesBufferLength = 200;
        int minLinesBufferLength = 200;
        SortingPhasePool pool =
            new SortingPhasePool(2, minBytesBufferLength, minLinesBufferLength);

        ReadingPhasePackage package1 = await pool.TryAcquireNextAsync();
        Task<ReadingPhasePackage> askingNextPackage = pool.TryAcquireNextAsync();
        Task timeLimit = Task.Delay(50);

        Task winner = await Task.WhenAny(askingNextPackage, timeLimit);

        Assert.Same(winner, timeLimit);

        pool.ReuseBuffer(package1.RowData);

        ReadingPhasePackage package3 = await askingNextPackage;
        Assert.Same(package3.RowData, package1.RowData);
    }
}