﻿using System.Text;
using Infrastructure.ByteOperations;
using SortingEngine;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngine.Utils;

namespace SortingEngineTests.RowData;

public class RecordsExtractorTests
{
    [Fact]
    public void RecordsExtractor_CannotBeCreatedWithoutProvidedEolAndDelimiter()
    {
        Assert.Throws<ArgumentNullException>(() => new RecordsExtractor(null, new byte[2]));
        Assert.Throws<ArgumentNullException>(() => new RecordsExtractor(new byte[] { 1 }, null));

        RecordsExtractor extractor = new RecordsExtractor(new byte[] { 1 }, new byte[] { 2 });
        Assert.NotNull(extractor);
    }

    [Fact]
    public void RecordsExtractor_ShouldRecognizeCorrectLines()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).Create(new[] { "12345. abcd EF*GH!", "678910. @ijk3 lm-NO PQ;" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

        Assert.True(result.Success);
        Assert.Equal(2, result.LinesNumber);
        Assert.Equal(input.Length - 12, result.StartRemainingBytes);

        string str1 = LineToString(linesStorage[0], input);
        Assert.Equal("12345. abcd EF*GH!" + Environment.NewLine, str1);
        string str2 = LineToString(linesStorage[1], input);
        Assert.Equal("678910. @ijk3 lm-NO PQ;" + Environment.NewLine, str2);
    }

    [Fact]
    public void RecordsExtractor_ShouldNotRecognizeLine_WithWrongDelimiter()
    {
        Encoding encoding = Encoding.UTF8;

        //comma instead of dot after the number
        byte[] input = DataGenerator.Use(encoding).Create(
            new[] { "12345. abcd EF*GH!", "678910, @ijk3 lm-NO PQ;", "8765. hdgf huhb" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: 678910, @ijk3 lm-NO PQ;" + Environment.NewLine, result.Message);
    }

    [Fact]
    public void RecordsExtractor_ShouldNotRecognizeLine_WithTooLongNumber()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).Create(
            new[] { "12345. abcd EF*GH!", "123456789012345678901. @ijk3 lm-NO PQ;", "8765. hdgf huhb" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: 123456789012345678901. @ijk3 lm-NO PQ;" + Environment.NewLine, result.Message);
    }

    private static RecordsExtractor GetUsual(Encoding encoding)
    {
        return new RecordsExtractor(encoding.GetBytes(Environment.NewLine), encoding.GetBytes(". "));
    }
    
    //todo temp
    private string LineToString(LineMemory line, byte[] source)
    {
        Span<byte> buffer = stackalloc byte[Constants.MaxLineLength_UTF8];
        int length = LineUtils.LineToBytes(line, source, buffer);
        
        return ByteToStringConverter.Convert(buffer[..length]);
    }
}