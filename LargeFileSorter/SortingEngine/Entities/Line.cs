namespace SortingEngine.Entities;

//todo benchmark use StructLayoutAttribute
public readonly record struct Line(ulong Number, int From, int To);