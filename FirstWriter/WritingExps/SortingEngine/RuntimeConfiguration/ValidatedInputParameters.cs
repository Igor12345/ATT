using System.Text;

namespace SortingEngine.RuntimeConfiguration;

public class ValidatedInputParameters
{
    private ValidatedInputParameters()
    {
    }

    public ValidatedInputParameters(string file, Encoding encoding)
    {
        //todo check values
        File = file;
        Encoding = encoding;
    }

    public Encoding Encoding { get; init; }

    public string File { get; init; }

    private static readonly ValidatedInputParameters _empty = new ValidatedInputParameters()
        { File = "", Encoding = Encoding.Default };
    public static ValidatedInputParameters Empty => _empty;
}