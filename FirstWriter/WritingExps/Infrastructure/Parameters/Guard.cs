using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Parameters
{
   public static class Guard
   {
      public static T NotNull<T>([DisallowNull] T value, string name)
      {
         return value ?? throw new ArgumentNullException(name);
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
   }
}