using FileCreator.Configuration;
using FileCreator.Lines;
using Infrastructure.Parameters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileCreator;

internal sealed class CreatingFileService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IRuntimeConfiguration _config;
    private ulong _currentLength;
    private int _linesToLog;
    private ulong _linesCount;

    public CreatingFileService(IRuntimeConfiguration config, ILogger<CreatingFileService> logger)
    {
        _config = Guard.NotNull(config);
        _logger = Guard.NotNull(logger);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started");

        //yep, I know about .Net Cora and DI, but in this case, that does not worth it
        ITextCreator basicTextCreator = new DefaultTextCreator(_config);
        ITextCreator textCreatorWithDuplicates = new DuplicatesTextCreator(_config, basicTextCreator);
        LineCreator lineCreator = new LineCreator(_config, textCreatorWithDuplicates);
        LinesGenerator generator = new LinesGenerator(lineCreator);
        await using LinesWriter linesWriter = LinesWriter.Create(_config, _logger);

        byte[] buffer = new byte[_config.MaxLineLength]; 
        foreach (int lineLength in generator.Generate(buffer.AsMemory()))
        {
            if (_linesToLog >= _config.LogEveryThsLine)
            {
                _linesToLog = 0;
                _logger.LogInformation("{lines} lines - {bytes} bytes.", _linesCount, _currentLength);
            }
            _linesToLog++;
            
            if(TimeToStop(lineLength, out int bytesToWrite))
                break;
            linesWriter.Write(buffer.AsSpan()[..bytesToWrite]);
            _linesCount++;
        }
        
        _logger.LogInformation("All lines created {lines} - {bytes} bytes.", _linesCount, _currentLength);
        _logger.LogInformation($"The file: {_config.FilePath}");
    }

    private bool TimeToStop(int lineLength, out int bytesToWrite)
    {
        bytesToWrite = lineLength;
        
        if (_config.FileSize <= _currentLength+(ulong)lineLength)
            return true;
        _currentLength += (ulong)lineLength;
        return false;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Done");
        return Task.CompletedTask;
    }
}