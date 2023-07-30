﻿namespace SortingEngine;

public record struct Error(string Message);

//There are many open-source libraries, for example: https://github.com/mcintyre321/OneOf
public record struct Result(bool Success, string Message)
{
   public static Result Ok = new(true, "");
   public static Result Error(string message) => new(false, message);
}

public record struct Result<T>(bool Success, T Value, string Message)
{
   public static Result<T> Ok(T value) => new(true, value, "");
   public static Result<T?> Error(string message) => new(false, default, message);
}

public record struct ReadingResult(bool Success, int Length, string Message)
{
   public static ReadingResult Ok(int length) => new(true, length, "");
   public static ReadingResult Error(string message) => new(false, -1, message);
}

public record struct ExtractionResult(bool Success, int LinesNumber, int StartRemainingBytes, string Message)
{
   public static ExtractionResult Ok(int linesNumber, int startRemainingBytes) =>
      new(true, linesNumber, startRemainingBytes, "");

   public static ExtractionResult Error(string message) => new(false, -1, -1, message);
}