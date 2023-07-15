using System.Collections;
using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngineTests.TestUtils;

namespace SortingEngineTests.Comparators;

public class OnSiteLinesComparerTests
{
    [Theory]
    [ClassData(typeof(LinesComparerTestData))]
    public void OnSiteLinesComparer_ShouldCorrectlyCompareLines((LineMemory[] Lines, byte[] Source, string[] Origin) input)
    {
        OnSiteLinesComparer comparer = new OnSiteLinesComparer(input.Source);

        Assert.True(string.Compare("abc", "abcd", StringComparison.Ordinal) < 0);
        Assert.True(string.Compare("abc", "def", StringComparison.Ordinal) < 0);
        Assert.True(string.Compare("abc", "Def", StringComparison.Ordinal) > 0);

        Assert.True(comparer.Compare(input.Lines[0], input.Lines[1]) < 0);
    }
}

public class LinesComparerTestData : IEnumerable<object[]>
{
    //todo support any encoding?
    public LinesComparerTestData() 
    {
    }

    //the original strings should be ordered correctly
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new[] { SimpleDataSet };
        yield return new[] { DifferentLengthDataSet };
        yield return new[] { CaseSensitiveDataSet };
        yield return new[] { WithRepeatingStringsDataSet };
    }

    private object SimpleDataSet
    {
        get
        {
            string[] origin = {
                "123. abc",
                "123. def"
            };
            var (lines, source) = DataGenerator.UTF8.CreateLinesFromStrings(origin);

            return (lines, source, origin);
        }
    }

    private object DifferentLengthDataSet
    {
        get
        {
            string[] origin = {
                "123. abc",
                "1233. abcd"
            };
            var (lines, source) = DataGenerator.UTF8.CreateLinesFromStrings(origin);

            return (lines, source, origin);
        }
    }

    private object CaseSensitiveDataSet
    {
        get
        {
            string[] origin = {
                "123. Def",
                "123. abc"
            };
            var (lines, source) = DataGenerator.UTF8.CreateLinesFromStrings(origin);

            return (lines, source, origin);
        }
    }

    private object WithRepeatingStringsDataSet
    {
        get
        {
            string[] origin = {
                "123. abc",
                "321. abc"
            };
            var (lines, source) = DataGenerator.UTF8.CreateLinesFromStrings(origin);

            return (lines, source, origin);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}