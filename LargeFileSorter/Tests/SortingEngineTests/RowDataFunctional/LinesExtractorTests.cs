using System.Text;
using LanguageExt;
using SortingEngine;
using SortingEngine.Algorithms;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowDataFunctional;
using SortingEngineTests.TestUtils;
using IParticularSubstringMatcher = SortingEngine.Algorithms.OnMemory.IParticularSubstringMatcher;

namespace SortingEngineTests.RowDataFunctional;

public class LinesExtractorTests
{
    public LinesExtractorTests()
    {
    }

    private readonly Encoding[] _encodings =
    {
        Encoding.UTF8,
        // Encoding.UTF32
    };

    [Fact]
    public void RecordsExtractor_ShouldRecognizeCorrectLines()
    {
        foreach (Encoding encoding in _encodings)
        {
            byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(
                new[] { "12345. abcπ ΣF*GH!", "678910. @ijk3 lm-Ππ Σσ;" },
                DataGenerator.RandomBytes(12));
            ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(500);
            LinesExtractor extractor = GetUsual(encoding);

            Either<Error, int> result = extractor.ExtractRecords(input, linesStorage);

            Assert.True(result.IsRight);
            Assert.Equal(input.Length - 12, result.Case);

            result.Match(
                Left: e => Assert.True(false),
                Right: tail => Assert.Equal(input.Length - 12, tail));

            Assert.Equal(2, linesStorage.Count);
            string str1 = LinesUtils.LineToString(linesStorage[0], input, encoding);
            Assert.Equal("12345. abcπ ΣF*GH!" + Environment.NewLine, str1);
            string str2 = LinesUtils.LineToString(linesStorage[1], input, encoding);
            Assert.Equal("678910. @ijk3 lm-Ππ Σσ;" + Environment.NewLine, str2);
        }
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

        var result = extractor.ExtractRecords(input, linesStorage);

        Assert.True(result.IsLeft);
        result.Match(
            Left: e => Assert.Equal(@"wrong line: 123def789. @ijk3Q;" + Environment.NewLine, e.Message),
            Right: tail => Assert.True(false));
    }

    [Fact]
    public void FoldTest()
    {
        var list = new List<Either<Error, int>>()
        {
            1, 
            3,
            Either<Error, int>.Left(new Error("2")),
            Either<Error, int>.Left(new Error("4")),
            5
        };
        var r = list.ToSeq().FoldTFast(0, (s, x) => s + x);
        var t = r;
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

        var result = extractor.ExtractRecords(input, linesStorage);

        Assert.True(result.IsLeft);
        result.Match(
            Left: e => Assert.Equal(@"wrong line: 123456789012345678901. @ijk3 lm-NO PQ;" + Environment.NewLine,
                e.Message),
            Right: tail => Assert.True(false));
    }

    private static LinesExtractor GetUsual(Encoding encoding)
    {
        LineParser parser = new LineParser(KmpMatcher.CreateForThisPattern(encoding.GetBytes(TestConstants.Delimiter)),
            encoding);
        IParticularSubstringMatcher eolFinder =
            SortingEngine.Algorithms.OnMemory.KmpMatcher.CreateForThisPattern(encoding.GetBytes(Environment.NewLine),
                true);
        return new LinesExtractor(eolFinder, parser);
    }
}