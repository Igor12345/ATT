using FileCreator.Configuration;

namespace FileCreator.Extensions;

internal static class ConfigurationExtensions
{
    public static IRuntimeConfiguration ToRuntime(this IBaseConfiguration baseConf)
    {
        ConfigurationValidator validator = new ConfigurationValidator();
        return validator.ProvideConfiguration(baseConf);
    }
}