using System.Text;
using SortingEngine.Algorithms;
using SortingEngine.RowData;

namespace SortingEngineTests.RowData;

public class LineParserTests
{
    
    private readonly Encoding[] _encodings = {
        Encoding.UTF8, 
        Encoding.ASCII, 
        Encoding.UTF32
    };
    private readonly string _pattern = ". ";

    [Theory]
    [InlineData("1234. apples", 1234, true)]
    [InlineData("1234. μήλα και μπανάνες", 1234, true)]
    [InlineData("-1234. apples", 0, false)]
    //ulong.MaxValue + 1
    [InlineData("18446744073709551616. apples", 0, false)]
    //ulong.MaxValue + '00'
    [InlineData("1844674407370955161005. apples", 0, false)]
    [InlineData("a45. apples", 0,false)]
    [InlineData("12345; bananas", 0,false)]
    [InlineData("123D. bananas", 0,false)]
    public void ShouldDoSearchPatternInBytesArray(string text, ulong number, bool validLine)
    {
        foreach (Encoding encoding in _encodings)
        {
            byte[] patternBytes = encoding.GetBytes(_pattern);
            byte[] textBytes = encoding.GetBytes(text);

            string[] parts = text.Split(_pattern);
            int expectedFrom = 0;
            int expectedTo = 0;
            if (validLine)
            {
                expectedFrom = encoding.GetByteCount(parts[0]);
                expectedTo = expectedFrom + encoding.GetByteCount(_pattern) + encoding.GetByteCount(parts[1]); //end of text in fact
            }
            
            LineParser parser = new LineParser(KmpMatcher.CreateForPattern(patternBytes), encoding);

            var result = parser.Parse(textBytes.AsSpan(), 0);
            Assert.True(validLine == result.Success, $"For encoding {encoding.EncodingName}");
            if (validLine)
            {
                Assert.True(number == result.Value.Number, $"Line number for encoding {encoding.EncodingName} Expected: {number} actual: {result.Value.Number}");
                Assert.True(expectedFrom == result.Value.From,
                    $"From position, for encoding {encoding.EncodingName} Expected: {expectedFrom} actual: {result.Value.From}");
                Assert.True(expectedTo == result.Value.To,
                    $"To position, for encoding {encoding.EncodingName} Expected: {expectedTo} actual: {result.Value.To}");
            }
        }
    }
}