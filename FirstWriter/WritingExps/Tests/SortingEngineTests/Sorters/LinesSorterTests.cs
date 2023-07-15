﻿using System.Collections;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.Sorters;
using SortingEngineTests.TestUtils;

namespace SortingEngineTests.Sorters;

public class LinesSorterTests
{
    [Theory]
    [ClassData(typeof(LinesSorterTestData))]
    public void LinesSorter_ShouldCorrectlySortLines((ExpandingStorage<LineMemory> LinesStorage, byte[] Source, string[] Origin) input)
    {
        LinesSorter sorter = new LinesSorter(input.Source);
        LineMemory[] sortedLines = sorter.Sort(input.LinesStorage, input.Origin.Length);

        string[] converted = sortedLines.Take(input.Origin.Length)
            .Select(line => LinesUtils.LineToString(line, input.Source)).ToArray();

        Action<string>[] asserts = input.Origin.Select<string, Action<string>>(str =>
        {
            return line => Assert.Equal(line, str + Environment.NewLine, false, false, false);
        }).ToArray();

        Assert.Collection(converted, asserts);
    }
}

public class LinesSorterTestData : IEnumerable<object[]>
{
    //todo support any encoding?
    public LinesSorterTestData() 
    {
    }

    //the original strings should be ordered correctly
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new[] { SimpleDataSet };
        yield return new[] { CaseSensitiveDataSet };
        yield return new[] { WithRepeatingStringsDataSet };
    }

    private object SimpleDataSet
    {
        get
        {
            string[] origin = {
                "123. abc",
                "123. abc;",
                "123. def"
            };
            ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(20);
            byte[] source =
                DataGenerator.UTF8.FillLinesStorageFromStrings(new[] { origin[1], origin[0], origin[2] }, linesStorage);

            return (linesStorage, source, origin);
        }
    }

    private object CaseSensitiveDataSet
    {
        get
        {
            string[] origin = {
                "123. Def",
                "323. abc",
                "234. abcd"
            };
            ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(20);
            byte[] source =
                DataGenerator.UTF8.FillLinesStorageFromStrings(new[] { origin[1], origin[0], origin[2] }, linesStorage);

            return (linesStorage, source, origin);
        }
    }

    private object WithRepeatingStringsDataSet
    {
        get
        {
            string[] origin = {
                "123. abc",
                "321. abc",
                "12. def"
            };
            ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(20);
            byte[] source =
                DataGenerator.UTF8.FillLinesStorageFromStrings(new[] { origin[1], origin[0], origin[2] }, linesStorage);

            return (linesStorage, source, origin);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}