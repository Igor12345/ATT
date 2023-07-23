using System.Text;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using SortingEngine.Algorithms;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class LineParser
{
    private readonly Encoding _encoding;
    private readonly IParticularSubstringMatcher _substringMatcher;
    private readonly int _maxLength;

    public LineParser(IParticularSubstringMatcher substringMatcher, Encoding encoding)
    {
        _substringMatcher = Guard.NotNull(substringMatcher);
        _encoding = Guard.NotNull(encoding);
        //todo move somewhere
        _maxLength = _encoding.GetByteCount(ulong.MaxValue.ToString());
    }

    public Result<Line> Parse(ReadOnlySpan<byte> lineSpan, int initialOffset)
    {
        Span<char> numberChars = stackalloc char[_maxLength];
        int endOfNumber = _substringMatcher.Find(lineSpan);

        if (endOfNumber > 0 && endOfNumber <= numberChars.Length)
        {
            _encoding.GetChars(lineSpan[..endOfNumber], numberChars);
            bool success = ulong.TryParse(numberChars, out var number);
            if (success)
            {
                return Result<Line>.Ok(new Line(number, initialOffset + endOfNumber, initialOffset + lineSpan.Length));
            }
        }

        return Result<Line>.Error($"wrong line: {ByteToStringConverter.Convert(lineSpan)}");
    }
}