﻿using System.Text;
using SortingEngine;
using SortingEngine.Algorithms;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngineTests.TestUtils;

namespace SortingEngineTests.RowData;

public class LinesExtractorTests
{
    
    private readonly Encoding[] _encodings = {
        Encoding.UTF8,  
        Encoding.UTF32
    };
    
    [Fact]
    public void RecordsExtractor_CannotBeCreatedWithoutProvidedEolAndDelimiter()
    {
        LineParser parser = new LineParser(KmpMatcher.CreateForThisPattern(new byte[2]), Encoding.Default);
        IParticularSubstringMatcher eolFinder = KmpMatcher.CreateForThisPattern(new byte[2]);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => new LinesExtractor(null, 2, parser));
        Assert.Throws<ArgumentNullException>(() => new LinesExtractor( eolFinder, 1, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        LinesExtractor extractor = new LinesExtractor(eolFinder, 1, parser);
        Assert.NotNull(extractor);
    }

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

            ExtractionResult result = extractor.ExtractRecords(input, linesStorage);

            Assert.True(result.Success);
            Assert.Equal(2, result.LinesNumber);
            Assert.Equal(input.Length - 12, result.StartRemainingBytes);

            string str1 = LinesUtils.LineToString(linesStorage[0], input, encoding);
            Assert.Equal("12345. abcπ ΣF*GH!" + Environment.NewLine, str1);
            string str2 = LinesUtils.LineToString(linesStorage[1], input, encoding);
            Assert.Equal("678910. @ijk3 lm-Ππ Σσ;" + Environment.NewLine, str2);
        }
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
        LineParser parser = new LineParser(KmpMatcher.CreateForThisPattern(encoding.GetBytes(TestConstants.Delimiter)),
            encoding);
        IParticularSubstringMatcher eolFinder = KmpMatcher.CreateForThisPattern(encoding.GetBytes(Environment.NewLine));
        return new LinesExtractor( eolFinder, encoding.GetBytes(Environment.NewLine).Length, parser);
    }
}