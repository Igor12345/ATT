namespace SimpleReader;

public record struct ReadingResult
{
   public bool Success;
   public long Size;
   public string Message;
}

public record struct Result(bool Success, string Message);
