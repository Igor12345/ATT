using System.Numerics;
using System.Runtime.CompilerServices;

namespace Infrastructure.Parameters
{
   public static class Guard
   {
      public static T NotNull<T>(T? value, [CallerArgumentExpression("value")] string paramName = null)
      {
         return value ?? throw new ArgumentNullException(paramName);
      }
      public static string NotNullOrEmpty(string? value, [CallerArgumentExpression("value")] string paramName = null)
      {
         return value ?? throw new ArgumentNullException(paramName);
      }

      public static string FileExist(string fileName)
      {
         if (File.Exists(fileName))
            return fileName;
         throw new InvalidOperationException($"The file {fileName} does not exist.");
      }

      public static string PathExist(string path)
      {
         if (Path.Exists(path))
            return path;
         throw new InvalidOperationException($"The path {path} does not exist.");
      }

      public static T Positive<T>(T value, [CallerArgumentExpression("value")] string paramName = null)
         where T : INumberBase<T>, IComparable<T>
      {
         if (value.CompareTo(T.Zero) <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value should be greater than zero.");
         return value;
      }
   }
}