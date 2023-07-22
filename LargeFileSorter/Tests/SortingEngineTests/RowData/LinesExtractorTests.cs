using System.Text;
using SortingEngine;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngineTests.TestUtils;

namespace SortingEngineTests.RowData;

public class LinesExtractorTests
{
    [Fact]
    public void RecordsExtractor_CannotBeCreatedWithoutProvidedEolAndDelimiter()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => new LinesExtractor(null, new byte[2]));
        Assert.Throws<ArgumentNullException>(() => new LinesExtractor(new byte[] { 1 }, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        LinesExtractor extractor = new LinesExtractor(new byte[] { 1 }, new byte[] { 2 });
        Assert.NotNull(extractor);
    }

    [Fact]
    public void RecordsExtractor_ShouldRecognizeCorrectLines()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(
            new[] { "12345. abcd EF*GH!", "678910. @ijk3 lm-NO PQ;" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(500);
        LinesExtractor extractor = GetUsual(encoding);

        ExtractionResult result = extractor.ExtractRecords(input, linesStorage);

        Assert.True(result.Success);
        Assert.Equal(2, result.LinesNumber);
        Assert.Equal(input.Length - 12, result.StartRemainingBytes);

        string str1 = LinesUtils.LineToString(linesStorage[0], input);
        Assert.Equal("12345. abcd EF*GH!" + Environment.NewLine, str1);
        string str2 = LinesUtils.LineToString(linesStorage[1], input);
        Assert.Equal("678910. @ijk3 lm-NO PQ;" + Environment.NewLine, str2);
    }

    [Fact]
    public void RecordsExtractor_ShouldNotRecognizeLine_WithWrongDelimiter()
    {
        Encoding encoding = Encoding.UTF8;

        //comma instead of dot after the number
        byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(
            new[] { "12345. abcd EF*GH!", "678910, @ijk3Q;", "8765. hdgf huhb" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(500);
        LinesExtractor extractor = GetUsual(encoding);

        ExtractionResult result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: 678910, @ijk3Q;" + Environment.NewLine, result.Message);
    }

    [Fact]
    public void RecordsExtractor_ShouldNotRecognizeLine_WithoutNumber()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(
            new[] { "12345. abcd EF*GH!", ". @ijk3Q;", "8765. hdgf huhb" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(500);
        LinesExtractor extractor = GetUsual(encoding);

        ExtractionResult result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: . @ijk3Q;" + Environment.NewLine, result.Message);
    }

    [Fact]
    public void RecordsExtractor_ShouldNotRecognizeLine_WithWrongNumber()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(
            new[] { "12345. abcd EF*GH!", "123def789. @ijk3Q;", "8765. hdgf huhb" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(500);
        LinesExtractor extractor = GetUsual(encoding);

        ExtractionResult result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: 123def789. @ijk3Q;" + Environment.NewLine, result.Message);
    }

    [Fact]
    public void RecordsExtractor_ShouldNotRecognizeLine_WithTooLongNumber()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(
            new[] { "12345. abcd EF*GH!", "123456789012345678901. @ijk3 lm-NO PQ;", "8765. hdgf huhb" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(500);
        LinesExtractor extractor = GetUsual(encoding);

        ExtractionResult result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: 123456789012345678901. @ijk3 lm-NO PQ;" + Environment.NewLine, result.Message);
    }

    private static LinesExtractor GetUsual(Encoding encoding)
    {
        return new LinesExtractor(encoding.GetBytes(Environment.NewLine), encoding.GetBytes(TestConstants.Delimiter));
    }
}

public class LineParserTests
{
    [Theory]
    [InlineData(". 4", "123. 456")]
    public void ShouldBuildCorrectPrefixFunction(string pattern, string text)
    {
        int index = KmpAlgorithm(pattern, text);

        var g = index;
        var r = pattern[1];
    }

    private static int KmpAlgorithm(string pattern, string text)
    {
        int[] prefix = BuildPrefix(pattern);
        int index;
        int q = 0;

        for (int i = 0; i < text.Length; i++)
        {
            while (q > 0 && pattern[q + 1] != text[i])
            {
                q = prefix[q];
            }

            if (pattern[q + 1] == text[i])
                q++;
            if (q == pattern.Length)
                index = i - pattern.Length;
        }

        index = -1;
        return index;
    }

    private static int[] BuildPrefix(string pattern)
    {
        int[] prefix = new int[pattern.Length];
        prefix[0] = 0;
        int index = 0;

        for (int i = 1; i < pattern.Length; i++)
        {
            int k = prefix[i - 1];
            while (pattern[k] != pattern[i] && k > 0)
            {
                k = prefix[k - 1];
            }
            if (pattern[k] == pattern[i])
            {
                prefix[i] = k + 1;
            }
            else
            {
                prefix[i] = 0;
            }
        }
        return prefix;
    }
}