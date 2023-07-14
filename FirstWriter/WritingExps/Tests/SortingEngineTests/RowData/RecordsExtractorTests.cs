using System.Text;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;

namespace SortingEngineTests.RowData;

public class RecordsExtractorTests
{
    [Fact]
    public void RecordsExtractorCannotBeCreatedWithoutEolAndDelimiter()
    {
        Assert.Throws<ArgumentNullException>(() => new RecordsExtractor(null, new byte[2]));
        Assert.Throws<ArgumentNullException>(() => new RecordsExtractor(new byte[] { 1 }, null));

        RecordsExtractor extractor = new RecordsExtractor(new byte[] { 1 }, new byte[] { 2 });
        Assert.NotNull(extractor);
    }

    [Fact]
    public void RecordsExtractorCanRecognizeLines()
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
    }

    private static RecordsExtractor GetUsual(Encoding encoding)
    {
        return new RecordsExtractor(encoding.GetBytes(Environment.NewLine), encoding.GetBytes(". "));
    }
}