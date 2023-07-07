namespace SimpleReader;

public readonly record struct LineRecord(long Number, byte[] Text);
public readonly record struct LineMemory(long Number, int From, int To);
public readonly record struct LineAsString(long Number, string Text);