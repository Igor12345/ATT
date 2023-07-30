namespace SortingEngine.Algorithms.OnMemory;

public class KmpMatcher : ISubstringMatcher
{
    public static IParticularSubstringMatcher CreateForThisPattern(byte[] pattern, bool includePattern)
    {
        var preBuiltPrefix = BuildPrefix(pattern);
        ParticularMatcher instance = new ParticularMatcher(pattern, preBuiltPrefix, includePattern);
        return instance;
    }

    private class ParticularMatcher : IParticularSubstringMatcher
    {
        private readonly bool _includePattern;
        private readonly byte[] _pattern;
        private readonly int[] _preBuiltPrefix;

        public ParticularMatcher(byte[] pattern, int[] preBuiltPrefix, bool includePattern)
        {
            _includePattern = includePattern;
            _pattern = NotNull(pattern);
            _preBuiltPrefix = NotNull(preBuiltPrefix);
        }
        public int Find(ReadOnlyMemory<byte> text)
        {
            int result = KmpAlgorithm(_pattern, text.Span, _preBuiltPrefix);

            return result >= 0
                ? result + (_includePattern ? _pattern.Length : 0)
                : result;
        }
    }

    public int Find(ReadOnlyMemory<byte> text, byte[] pattern)
    {
        int[] prefix = BuildPrefix(pattern);
        return KmpAlgorithm(pattern, text.Span, prefix);
    }

    // https://www.geeksforgeeks.org/kmp-algorithm-for-pattern-searching/
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
            }
            else if (pr != 0)
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

    private static int KmpAlgorithm(byte[] pattern, ReadOnlySpan<byte> text, int[] prefix)
    {
        int p = 0;
        int t = 0;
        int textLength = text.Length;
        int patternLength = pattern.Length;

        while (t < textLength)
        {
            if (text[t] == pattern[p])
            {
                p++;
                t++;
            }
            else
            {
                if (p == 0)
                    t++;
                else
                    p = prefix[p - 1];
            }

            if (p == patternLength)
                return t - patternLength;
        }

        return -1;
    }
}