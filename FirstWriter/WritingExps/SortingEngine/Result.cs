namespace SortingEngine;

public record struct Result(bool Success, string Message)
{
   public static Result Ok = new Result(true, "");
   public static Result Error(string message) => new Result(false, message);
}

public record struct ReadingResult(bool Success, int Size, string Message)
{
   public static ReadingResult Ok(int size) => new ReadingResult(true, size, "");
   public static ReadingResult Error(string message) => new ReadingResult(false, -1, message);
}

public record struct ExtractionResult(bool Success, int Size, int StartRemainingBytes, string Message)
{
   public static ExtractionResult Ok(int size, int startRemainingBytes) =>
      new ExtractionResult(true, size, startRemainingBytes, "");

   public static ExtractionResult Error(string message) =>
      new ExtractionResult(false, -1, -1, message);
}