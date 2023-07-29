namespace SortingEngine.Algorithms;

public interface ISubstringMatcher
{
    int Find(ReadOnlySpan<byte> text, byte[] pattern);
}
public interface IParticularSubstringMatcher
{
    int Find(ReadOnlySpan<byte> text);
}

public class KmpMatcher : ISubstringMatcher
{
    public static IParticularSubstringMatcher CreateForThisPattern(byte[] pattern)
    {
        var preBuiltPrefix = BuildPrefix(pattern);
        ParticularMatcher instance = new ParticularMatcher(pattern, preBuiltPrefix);
        return instance;
    }

    private class ParticularMatcher : IParticularSubstringMatcher
    {
        private readonly byte[] _pattern;
        private readonly int[] _preBuiltPrefix;

        public ParticularMatcher(byte[] pattern, int[] preBuiltPrefix)
        {
            _pattern = NotNull(pattern);
            _preBuiltPrefix = NotNull(preBuiltPrefix);
        }
        public int Find(ReadOnlySpan<byte> text)
        {
            return KmpAlgorithm(_pattern, text, _preBuiltPrefix);
        }
    }

    public int Find(ReadOnlySpan<byte> text, byte[] pattern)
    {
        int[] prefix = BuildPrefix(pattern);
        return KmpAlgorithm(pattern, text, prefix);
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