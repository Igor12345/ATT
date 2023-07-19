﻿using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Sorting;

namespace SortingEngine;

public class SetOfLinesSorter
{
    private readonly Func<ReadOnlyMemory<byte>, ILinesSorter> _sorterFactory;
    private readonly ILogger _logger;

    public SetOfLinesSorter(ILogger logger, Func<ReadOnlyMemory<byte>, ILinesSorter> sorterFactory)
    {
        _logger = Guard.NotNull(logger);
        _sorterFactory = Guard.NotNull(sorterFactory);
    }

    public async Task<AfterSortingPhasePackage> ProcessPackageAsync(SortingPhasePackage package)
    {
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.OccupiedLength];
        Line[] sorted = SortRecords(inputBytes, package.LinesNumber, package.ParsedRecords);

        await Log(
            $"Sorted lines: {package.LinesNumber}) " +
            $"for the package: {package.PackageNumber}, sending AfterSortingPhasePackage");
        return new AfterSortingPhasePackage(sorted, package.RowData,
            package.ParsedRecords, package.LinesNumber, package.PackageNumber, package.IsLastPackage);
    }

    private Line[] SortRecords(ReadOnlyMemory<byte> inputBuffer, int linesNumber,
        ExpandingStorage<Line> recordsStorage)
    {
        //In the case of such a highly specific line comparison algorithm,
        //it makes no sense to add an interface and use DI
        //There is no chance that any other sorter will be within the scope of this task.
        //A mock for testing, in this case, is also not needed.
        ILinesSorter sorter = _sorterFactory(inputBuffer);
        return sorter.Sort(recordsStorage, linesNumber);
    }

    // in a real project, working with logs will look completely different
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"{GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff}, ({Thread.CurrentThread.ManagedThreadId})";
        await _logger.LogAsync(prefix + message);
    }
}