using System.Text;
using Infrastructure.ByteOperations;
using SortingEngine;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngineTests.TestUtils;

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

    //todo delete
    [Fact]
    public void Exp()
    {
        Encoding encoding = Encoding.UTF8;
        byte[] input = new byte[] { 65, 75, 93, 83, 124, 69, 125, 75, 67, 105, 87, 97, 112, 105, 77, 87, 84, 67, 118, 77, 86, 108, 108, 97, 69, 82, 120, 110, 120, 124, 74, 100, 81, 99, 93, 79, 92, 115, 112, 68, 66, 100, 112, 125, 122, 88, 112, 114, 117, 81, 84, 80, 72, 76, 125, 83, 93, 122, 79, 13, 10, 49, 48, 48, 53, 52, 52, 50, 57, 49, 48, 49, 49, 57, 49, 49, 57, 51, 49, 50, 46, 32, 106, 75, 80, 80, 69, 96, 112, 80, 78, 77, 124, 120, 94, 81, 115, 75, 111, 122, 108, 100, 107, 95, 70, 118, 103, 104, 120, 93, 69, 99, 74, 111, 96, 101, 70, 119, 112, 84, 108, 94, 74, 72, 98, 102, 119, 87, 115, 124, 83, 66, 78, 86, 111, 86, 98, 76, 70, 87, 102, 90, 101, 84, 75, 104, 67, 77, 89, 119, 75, 121, 88, 91, 79, 79, 90, 99, 122, 93, 124, 81, 114, 121, 74, 68, 86, 114, 65, 75, 68, 120, 114, 90, 108, 82, 118, 116, 122, 120, 100, 107, 124, 122, 114, 87, 104, 112, 72, 97, 121, 124, 79, 102, 89, 86, 101, 106, 74, 72, 83, 78, 76, 79, 115, 80, 72, 124, 109, 122, 86, 99, 102, 67, 118, 81, 83, 118, 71, 113, 90, 99, 106, 94, 79, 69, 106, 76, 65, 86, 107, 77, 107, 115, 101, 86, 96, 110, 71, 98, 79, 105, 68, 76, 75, 86, 87, 76, 110, 72, 68, 109, 99, 87, 108, 109, 98, 107, 125, 111, 100, 122, 123, 87, 80, 74, 118, 113, 91, 96, 110, 99, 111, 99, 98, 110, 96, 94, 83, 118, 108, 94, 91, 119, 83, 123, 103, 85, 71, 117, 114, 114, 110, 86, 123, 83, 108, 106, 122, 66, 94, 99, 110, 78, 87, 95, 123, 125, 115, 124, 78, 68, 88, 72, 67, 71, 95, 69, 69, 82, 96, 98, 107, 100, 108, 121, 114, 90, 102, 79, 67, 116, 98, 82, 88, 73, 109, 78, 96, 114, 65, 103, 95, 73, 104, 110, 91, 102, 83, 117, 113, 104, 101, 104, 95, 122, 118, 75, 96, 91, 106, 97, 88, 100, 122, 87, 95, 71, 109, 120, 87};
        
        var str = ByteToStringConverter.Convert(input);
        
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);
        var t = result;
    }

    [Fact]
    public void RecordsExtractor_ShouldRecognizeCorrectLines()
    {
        Encoding encoding = Encoding.UTF8;

        byte[] input = DataGenerator.Use(encoding).CreateWholeBytes(new[] { "12345. abcd EF*GH!", "678910. @ijk3 lm-NO PQ;" },
            DataGenerator.RandomBytes(12));
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

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
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

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
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

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
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

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
        ExpandingStorage<LineMemory> linesStorage = new ExpandingStorage<LineMemory>(500);
        RecordsExtractor extractor = GetUsual(encoding);

        var result = extractor.ExtractRecords(input, linesStorage);

        Assert.False(result.Success);
        Assert.Equal(@"wrong line: 123456789012345678901. @ijk3 lm-NO PQ;" + Environment.NewLine, result.Message);
    }

    private static RecordsExtractor GetUsual(Encoding encoding)
    {
        return new RecordsExtractor(encoding.GetBytes(Environment.NewLine), encoding.GetBytes(Constants.Delimiter));
    }

    //todo temp
    
}