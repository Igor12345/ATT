namespace SortingEngine.RowDataFunctional;

public static class SpanExtensions{
    public static IEnumerable<Either<int, PositionedSpan<T>>> SplitOn<T>(this ReadOnlyMemory<T> span,
        Func<ReadOnlyMemory<T>, int> splitCondition)
    {
        int startLine = 0;
        do
        {
            int endCurrentLineNoEol= splitCondition(span[startLine..]);

            if (endCurrentLineNoEol < 0)
            {
                yield return startLine;
                break;
            }

            if (endCurrentLineNoEol == 0)
                throw new InvalidOperationException("Something wrong during seeking the next eol or empty line.");
            
            int endLine = endCurrentLineNoEol;
            yield return new PositionedSpan<T>(span, startLine, endLine);
            startLine += endLine;

        } while (true);
    }

    public static Either<Error, S> FoldTFast<T, S>(this IEnumerable<Either<Error, T>> seq, S state,
        Func<S, T, S> f)
    {
        Either<Error, S> result = state;
        foreach (Either<Error, T> item in seq)
        {
            if (item.IsLeft)
                return (Error)item.Case;

            result = item.Match(
                Left: e => (Either<Error, S>)e,
                Right: next => state = f(state, next)
            );
        }

        return result;
    }
}