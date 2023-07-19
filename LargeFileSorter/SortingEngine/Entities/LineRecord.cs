namespace SortingEngine.Entities;

public readonly record struct LineRecord(ulong Number, byte[] Text);
public readonly record struct LineAsString(ulong Number, string Text);

public readonly record struct LineMemory(ulong Number, int From, int To);