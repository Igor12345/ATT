using System.Text;
using Infrastructure.ByteOperations;
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
        _substringMatcher = NotNull(substringMatcher);
        _encoding = NotNull(encoding);
        //todo move somewhere
        _maxLength = _encoding.GetByteCount(ulong.MaxValue.ToString());
    }

    public Result<Line> Parse(ReadOnlySpan<byte> lineSpan)
    {
        Span<char> numberChars = stackalloc char[_maxLength];
        int endOfNumber = _substringMatcher.Find(lineSpan);

        if (endOfNumber > 0 && endOfNumber <= numberChars.Length)
        {
            _encoding.GetChars(lineSpan[..endOfNumber], numberChars);
            bool success = ulong.TryParse(numberChars, out var number);
            if (success)
            {
                return Result<Line>.Ok(new Line(number, endOfNumber, lineSpan.Length));
            }
        }

        return Result<Line>.Error($"wrong line: {ByteToStringConverter.Convert(lineSpan, _encoding)}");
    }
}