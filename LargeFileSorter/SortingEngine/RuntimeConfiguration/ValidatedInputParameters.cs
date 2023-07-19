using System.Text;

namespace SortingEngine.RuntimeConfiguration;

public class ValidatedInputParameters
{
    public ValidatedInputParameters(string file, Encoding encoding)
    {
        //todo check values
        File = file;
        Encoding = encoding;
    }

    public Encoding Encoding { get; }

    public string File { get; }

    private static readonly ValidatedInputParameters _empty = new("", Encoding.Default);
    public static ValidatedInputParameters Empty => _empty;
}