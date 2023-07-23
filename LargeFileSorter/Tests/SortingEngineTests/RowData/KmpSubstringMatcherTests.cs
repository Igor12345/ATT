using System.Text;
using SortingEngine.Algorithms;

namespace SortingEngineTests.RowData;

public class KmpSubstringMatcherTests
{
    private readonly Encoding[] _encodings = {
        Encoding.UTF8, 
        Encoding.ASCII, 
        Encoding.UTF32
    };

    [Theory]
    [InlineData("efg", "abc defg 5", 5)]
    [InlineData(". ", "αΨβΩσ. efg 5", 5)]
    [InlineData("bca", "abc defg 5", -1)]
    [InlineData("24", "ab24c de24fg 5", 2)]
    [InlineData(". ", "1234. de24fg 5", 4)]
    public void ShouldDoSearchPatternInBytesArray(string pattern, string text, int expectedIndex)
    {
        foreach (Encoding encoding in _encodings)
        {
            byte[] patternBytes = encoding.GetBytes(pattern);
            byte[] textBytes = encoding.GetBytes(text);
            int expectedAt = expectedIndex;
            if (expectedIndex >= 0)
            {
                expectedAt = encoding.GetByteCount(text[..expectedIndex]);
            }
            
            IParticularSubstringMatcher patternSeeker = KmpMatcher.CreateForThisPattern(patternBytes);
            int index = patternSeeker.Find(textBytes);
            
            Assert.Equal(expectedAt, index);
        }
    }
}