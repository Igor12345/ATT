using System.Text;

namespace ContentGenerator;

public interface ITextCreator
{
   IEnumerable<byte[]> GenerateBytes();
   IEnumerable<string> GenerateStrings();
   Encoding Encoding { get; }
}