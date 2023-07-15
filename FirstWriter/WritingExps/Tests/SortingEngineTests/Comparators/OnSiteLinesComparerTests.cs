using System.Collections;
using System.Text;
using SortingEngine.Comparators;
using SortingEngine.Entities;
using SortingEngineTests.TestUtils;
using Guard = Infrastructure.Parameters.Guard;

namespace SortingEngineTests.Comparators;

public class OnSiteLinesComparerTests
{
    [Theory]
    [ClassData(typeof(LinesComparerTestData))]
    public void OnSiteLinesComparer_ShouldCorrectlyCompareLines((LineMemory[] lines, byte[] source, string[] origin) input)
    {
        (LineMemory[] lines, ReadOnlyMemory<byte> source, string[] origin) t = input;

        OnSiteLinesComparer comparer = new OnSiteLinesComparer(input.source);

        Assert.True(string.Compare("abc", "def", StringComparison.Ordinal) < 0);
        Assert.True(string.Compare("abc", "Def", StringComparison.Ordinal) > 0);

        Assert.True(comparer.Compare(input.lines[0], input.lines[1]) < 0);
    }
    
    [Theory]
    [ClassData(typeof(LinesComparerTestData))]
    public void OnSiteLinesSorter_ShouldCorrectlySortLines((LineMemory[] lines, byte[] source, string[] origin) input)
    {
        (LineMemory[] lines, ReadOnlyMemory<byte> source, string[] origin) t = input;

        
        string[] converted = input.lines.Select(line => LinesUtils.LineToString(line, input.source)).ToArray();

        Action<string>[] asserts = input.origin.Select<string, Action<string>>(str =>
        {
            return line => Assert.Equal(line, str, false, false, false);
        }).ToArray();

        Assert.Collection(converted, asserts);
    }
}

public class LinesComparerTestData : IEnumerable<object[]>
{
    private readonly byte[] _eol;
    private readonly byte[] _delimiter;

    //todo support any encoding?
    public LinesComparerTestData() : this(Encoding.UTF8)
    {
    }

    public LinesComparerTestData(Encoding encoding)
    {
        var _ = Guard.NotNull(encoding);
        _eol = encoding.GetBytes(Environment.NewLine);
        _delimiter = encoding.GetBytes(". ");
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
                "123. def"
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