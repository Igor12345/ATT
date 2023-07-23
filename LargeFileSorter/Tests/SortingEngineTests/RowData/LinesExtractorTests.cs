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
    // https://www.geeksforgeeks.org/kmp-algorithm-for-pattern-searching/
    [Theory]
    [InlineData("aaaa", new byte[]{0,1,2,3})]
    [InlineData("abc", new byte[]{0,0,0})]
    [InlineData("AABAACAABAA", new byte[]{0, 1, 0, 1, 2, 0, 1, 2, 3, 4, 5})]
    [InlineData("AAACAAAAAC", new byte[]{0, 1, 2, 0, 1, 2, 3, 3, 3, 4})]
    [InlineData("AAABAAA", new byte[]{0, 1, 2, 0, 1, 2, 3})]
    public void ShouldBuildCorrectPrefixFunction(string pattern, byte[] prefixResult)
    {
        var prefix = BuildPrefix(pattern);

        for (int i = 0; i < pattern.Length; i++)
        {
            Assert.Equal(prefixResult[i], prefix[i]);
        }
    }

    [Theory]
    [InlineData("efg", "abc defg 5", 5)]
    [InlineData("bca", "abc defg 5", -1)]
    [InlineData("24", "ab24c de24fg 5", 2)]
    [InlineData(". ", "1234. de24fg 5", 4)]
    public void ShouldDoSearchPatternInString(string pattern, string text, int index)
    {
        var prefix = BuildPrefix(pattern);
        int foundAt = KmpAlgorithm(pattern, text, prefix);
        Assert.Equal(index, foundAt);
    }
    
    [Theory]
    [InlineData("efg", "abc defg 5", 5)]
    [InlineData("bca", "abc defg 5", -1)]
    [InlineData("24", "ab24c de24fg 5", 2)]
    [InlineData(". ", "1234. de24fg 5", 4)]
    public void ShouldDoSearchPatternInBytesArray(string pattern, string text, int index)
    {
        Encoding encoding = Encoding.UTF8;
        var patternBytes = encoding.GetBytes(pattern);
        var textBytes = encoding.GetBytes(text);
        var prefix = BuildPrefix(patternBytes);
        int foundAt = KmpAlgorithm(patternBytes, textBytes, prefix);
        Assert.Equal(index, foundAt);
    }

    private static int KmpAlgorithm(string pattern, string text, int[] prefix)
    {
        int p = 0;
        int t = 0;

        while (t < text.Length)
        {
            if (text[t] == pattern[p])
            {
                p++;
                t++;
            }
            else
            {
                if (p == 0)
                {
                    t++;
                }
                else
                {
                    p = prefix[t - 1];
                }
            }

            if (p == pattern.Length)
            {
                return t - pattern.Length;
            }
        }

        return -1;
    }

    private static int[] BuildPrefix(string pattern)
    {
        int ln = pattern.Length;
        int[] prefix = new int[ln];
        prefix[0] = 0;
        int index = 1;
        int pr = 0;

        while (index < ln)
        {
            if (pattern[index] == pattern[pr])
            {
                pr++;
                prefix[index] = pr;
                index++;
            }else if (pr != 0)
            {
                pr = prefix[pr - 1];
            }
            else
            {
                prefix[index] = 0;
                index++;
            }
        }

        return prefix;
    }
    
    private static int[] BuildPrefix(byte[] pattern)
    {
        int ln = pattern.Length;
        int[] prefix = new int[ln];
        prefix[0] = 0;
        int index = 1;
        int pr = 0;

        while (index < ln)
        {
            if (pattern[index] == pattern[pr])
            {
                pr++;
                prefix[index] = pr;
                index++;
            }else if (pr != 0)
            {
                pr = prefix[pr - 1];
            }
            else
            {
                prefix[index] = 0;
                index++;
            }
        }

        return prefix;
    }
    
    private static int KmpAlgorithm(byte[] pattern, byte[] text, int[] prefix)
    {
        int p = 0;
        int t = 0;

        while (t < text.Length)
        {
            if (text[t] == pattern[p])
            {
                p++;
                t++;
            }
            else
            {
                if (p == 0)
                {
                    t++;
                }
                else
                {
                    p = prefix[t - 1];
                }
            }

            if (p == pattern.Length)
            {
                return t - pattern.Length;
            }
        }

        return -1;
    }
}