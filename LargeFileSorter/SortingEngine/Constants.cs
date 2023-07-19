namespace SortingEngine;

public class Constants
{
   //todo works only for UTF-8
   // public static readonly int MaxTextLength = 3;
   public static readonly int MaxTextLength = 1024;
   // public static readonly int MaxNumberLength = 3;
   public static readonly int MaxNumberLength = 20;
   public static readonly int MaxLineLengthUtf8 = MaxNumberLength + MaxTextLength + 2 + 2;
   public static readonly int MaxStackLimit  = 1048;

   public const string Delimiter = ". ";
}