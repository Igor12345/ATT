namespace SortingEngine;

public record struct Result(bool Success, string Message)
{
   public static Result Ok = new Result(true, "");
   public static Result Error(string message) => new Result(false, message);
}

public record struct Result<T>(bool Success, T Value, string Message)
{
   public static Result<T> Ok(T value) => new Result<T>(true, value, "");
   public static Result<T> Error(string message) => new Result<T>(false, default, message);
}

public record struct ReadingResult(bool Success, int Size, string Message)
{
   public static ReadingResult Ok(int size) => new ReadingResult(true, size, "");
   public static ReadingResult Error(string message) => new ReadingResult(false, -1, message);
}

public record struct ExtractionResult(bool Success, int LinesNumber, int StartRemainingBytes, string Message)
{
   public static ExtractionResult Ok(int linesNumber, int startRemainingBytes) =>
      new ExtractionResult(true, linesNumber, startRemainingBytes, "");

   public static ExtractionResult Error(string message) =>
      new ExtractionResult(false, -1, -1, message);
}