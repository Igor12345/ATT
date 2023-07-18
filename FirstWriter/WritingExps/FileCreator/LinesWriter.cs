using FileCreator.Configuration;
using Infrastructure.Parameters;
using Microsoft.Extensions.Logging;

namespace FileCreator;

public sealed class LinesWriter : IDisposable, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IRuntimeConfiguration _config;
    private FileStream _fileStream = null!;

    private LinesWriter(IRuntimeConfiguration config, ILogger logger)
    {
        _config = Guard.NotNull(config);
        _logger = Guard.NotNull(logger);
    }

    public static LinesWriter Create(IRuntimeConfiguration config, ILogger logger)
    {
        LinesWriter instance = new LinesWriter(config, logger);
        instance.Initialize();
        return instance;
    }

    private void Initialize()
    {
        _fileStream = new FileStream(_config.FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, false);
    }

    public void Write(Span<byte> buffer)
    {
        //let's propagate exception. It looks like there is nothing that we can di in this case.
        _fileStream.Write(buffer);
    }

    public void Dispose()
    {
        _fileStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _fileStream.DisposeAsync();
    }
}