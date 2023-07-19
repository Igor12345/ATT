using System.Text;

namespace FileCreator.Configuration;

//can be internal, need to add InternalsVisibleToAttribute to this assembly, make it friendly for tests
//
public class ConfigurationValidator
{
    private readonly IRuntimeConfiguration _defaultConf;
    private readonly Dictionary<string, Func<double, ulong>> _fileSizes;

    public ConfigurationValidator() : this(new RuntimeConfiguration())
    {
        ulong kb = 1024;
        _defaultConf = new RuntimeConfiguration();
        _fileSizes = new()
        {
            { "Kb", v => (ulong)(v * kb) },
            { "Mb", v => (ulong)(v * kb * kb) },
            { "Gb", v => (ulong)(v * kb * kb * kb) }
        };
    }

    //questionable decision, just to show one of possible ways for testing 
    protected ConfigurationValidator(IRuntimeConfiguration defaultConf)
    {
        _defaultConf = defaultConf;
        ulong kb = 1024;
        _fileSizes = new()
        {
            { "Kb", v => (ulong)v * kb },
            { "Mb", v => (ulong)v * kb * kb },
            { "Gb", v => (ulong)v * kb * kb * kb }
        };
    }
    
    public IRuntimeConfiguration ProvideConfiguration(IBaseConfiguration baseConf)
    {
        //there are 
        EncodingInfo? encodingInfo = Encoding
            .GetEncodings().FirstOrDefault(e => e.Name.Equals(baseConf.Encoding, StringComparison.InvariantCultureIgnoreCase));

        Encoding encoding = encodingInfo != null
            ? Encoding.GetEncoding(encodingInfo.Name)
            : _defaultConf.Encoding;

        string delimiter = string.IsNullOrEmpty(baseConf.Delimiter) 
            ? _defaultConf.Delimiter 
            : baseConf.Delimiter;
        string possibleCharacters = string.IsNullOrEmpty(baseConf.PossibleCharacters)
            ? _defaultConf.PossibleCharacters
            : baseConf.PossibleCharacters;
        int ulongLength = 20;
        int delimiterLength = encoding.GetBytes(delimiter).Length;
        int eolLength = encoding.GetBytes(Environment.NewLine).Length;
        int maxLineLength = baseConf.MaxLineLength.HasValue
            ? Math.Max(ulongLength + delimiterLength + eolLength, baseConf.MaxLineLength.Value)
            : _defaultConf.MaxLineLength;
        int maxTextLength = baseConf.MaxTextLength.HasValue
            ? Math.Min(baseConf.MaxTextLength.Value, maxLineLength - delimiterLength - eolLength)
            : _defaultConf.MaxTextLength;
        int seed = baseConf.RandomSeed ?? _defaultConf.Seed;
        
        string[] samples = baseConf.Samples ?? _defaultConf.Samples;
        int duplicationFrequency = Math.Max(baseConf.DuplicationFrequency ?? _defaultConf.DuplicationFrequency,
            samples.Length + 10);
        
        ulong fileSize = ParseFileSize(baseConf.FileSize);
        var logEveryThsLine = baseConf.LogEveryThsLine ?? _defaultConf.LogEveryThsLine;

        string filePath = GetFileAndFolder(baseConf);

        RuntimeConfiguration configuration = new RuntimeConfiguration()
        {
            Delimiter = delimiter,
            PossibleCharacters = possibleCharacters,
            DuplicationFrequency = duplicationFrequency,
            Encoding = encoding,
            FilePath = filePath,
            FileSize = fileSize,
            LogEveryThsLine = logEveryThsLine,
            MaxLineLength = maxLineLength,
            MaxTextLength = maxTextLength,
            Samples = samples,
            Seed = seed
        };
        return configuration;
    }

    //in the case of wrong either file or folder, or both will be used default values
    private string GetFileAndFolder(IBaseConfiguration baseConf)
    {
        string fileName = string.IsNullOrEmpty(baseConf.FileName)
                    ? _defaultConf.FilePath
                    : baseConf.FileName;
        string folder;
        if (string.IsNullOrEmpty(baseConf.OutputDirectory))
            folder = Directory.GetCurrentDirectory();
        else if(baseConf.OutputDirectory.Equals("temp", StringComparison.InvariantCultureIgnoreCase))
        {
            folder = Path.GetTempPath();
        }
        else
        {
            folder = baseConf.OutputDirectory;
        }

        string fullPath = Path.Combine(folder, fileName);

        if (Path.IsPathFullyQualified(fullPath))
            return fullPath;

        return Path.Combine(Directory.GetCurrentDirectory(), _defaultConf.FilePath);
    }

    private ulong ParseFileSize(string? sizeToParse)
    {
        if (string.IsNullOrEmpty(sizeToParse))
            return _defaultConf.FileSize;

        foreach (var unit in _fileSizes)
        {
            int unitPosition = sizeToParse.IndexOf(unit.Key, StringComparison.InvariantCultureIgnoreCase);
            if (unitPosition > 0 && double.TryParse(sizeToParse[..unitPosition], out var result))
                return unit.Value(result);
        }

        if (ulong.TryParse(sizeToParse, out var size))
            return size;
        return _defaultConf.FileSize;
    }
}