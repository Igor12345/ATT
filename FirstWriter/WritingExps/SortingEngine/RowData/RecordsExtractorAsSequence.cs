using System.Buffers;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Infrastructure.ByteOperations;
using Infrastructure.Parameters;
using LogsHub;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public sealed class RecordsExtractorAsSequence : IAsyncObserver<ReadingPhasePackage>
{
    private readonly CancellationToken _token;
    private readonly Channel<int> _output = Channel.CreateBounded<int>(1);
    private readonly Channel<ReadOnlyMemory<byte>> _input = Channel.CreateBounded<ReadOnlyMemory<byte>>(1);
    private readonly Channel<string> _error = Channel.CreateUnbounded<string>();

    private readonly SimpleAsyncSubject<PreReadPackage> _readyForNextChunkSubject =
        new SequentialSimpleAsyncSubject<PreReadPackage>();
    private readonly SimpleAsyncSubject<SortingPhasePackage> _readyForSortingSubject = 
        new SequentialSimpleAsyncSubject<SortingPhasePackage>();
      
    private readonly byte[] _eol;
    private readonly byte[] _lineDelimiter;
    private readonly Logger _logger;

    public RecordsExtractorAsSequence(byte[] eol, byte[] lineDelimiter, Logger logger, CancellationToken token)
    {
        _eol = eol;
        _lineDelimiter = lineDelimiter;
        _logger = Guard.NotNull(logger);
        _token = Guard.NotNull(token);
    }

    public IAsyncObservable<PreReadPackage> ReadyForNextChunk => _readyForNextChunkSubject;
    public IAsyncObservable<SortingPhasePackage> ReadyForSorting => _readyForSortingSubject;

    // [Conditional("VERBOSE")]
    // private void Log(string message)
    // {
    //     _logger.LogAsync(message);
    // }

    private async Task ExtractNext(ReadingPhasePackage package)
    {
        await Log(
            $"Processing package: {package.PackageNumber}, is last: {package.IsLastPackage}, bytes: {package.RowData.Length}, prepopulated: {package.PrePopulatedBytesLength}");
        if (package.IsLastPackage)
        {
            SortingPhasePackage lastPackage = new SortingPhasePackage(package.RowData, package.ReadBytesLength,
                package.ParsedRecords, 0, package.PackageNumber, true);
            await Log($"Sending the last package without processing: {package.PackageNumber}");
            await _readyForSortingSubject.OnNextAsync(lastPackage);
            return;
        }
        
        ReadOnlyMemory<byte> inputBytes = package.RowData.AsMemory()[..package.ReadBytesLength];
        ExtractionResult result = ExtractRecords(inputBytes.Span, package.ParsedRecords);
        
        if (!result.Success)
        {
            await Log($"Extracted {result.Success}: {result.Message} ");
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
        }

        int remainingBytesLength = package.ReadBytesLength - result.StartRemainingBytes;

        byte[] remainedBytes = ArrayPool<byte>.Shared.Rent(remainingBytesLength);
        package.RowData.AsSpan()[result.StartRemainingBytes..package.ReadBytesLength].CopyTo(remainedBytes);

        //todo
        SortingPhasePackage nextPackage = new SortingPhasePackage(package.RowData, package.ReadBytesLength,
            package.ParsedRecords, result.LinesNumber, package.PackageNumber);

        await Log(
            $"Sending the package {nextPackage.PackageNumber}, extracted {nextPackage.LinesNumber}, bytes: {nextPackage.RowData.Length}, linesBuffer: {nextPackage.ParsedRecords.CurrentCapacity}, used bytes: {nextPackage.OccupiedLength}");
        //todo!!!
        var t1 = _readyForSortingSubject.OnNextAsync(nextPackage);
        await Log(
            $"Sending the package {nextPackage.PackageNumber}, after 1 - ReadyForSorting");
        
        var t2 = _readyForNextChunkSubject.OnNextAsync(new PreReadPackage(remainedBytes,
            remainingBytesLength));
        await Log(
            $"Sending the package {nextPackage.PackageNumber}, after 2 - ReadyForNextChunk");
        await Task.WhenAll(t1.AsTask(), t2.AsTask());
        await Log(
            $"Sending the package {nextPackage.PackageNumber}, after all");
    }

    public async Task WaitingForNextPartAsync()
    {
        await _output.Writer.WriteAsync(2, CancellationToken.None);
    }

    public async Task ExtractRecords()
    {
        var consumer = ExtractRecords(_input.Reader);
    }

    private async Task ExtractRecords(ChannelReader<ReadOnlyMemory<byte>> reader)
    {
        ReadOnlyMemory<byte> chunk = await reader.ReadAsync(_token);
         
    }

    private ExtractionResult ExtractRecords(ReadOnlySpan<byte> input, ExpandingStorage<LineMemory> records)
    {
        int lineIndex = 0;
        int endLine = 0;
        int endOfLastLine = -1;
        for (int i = 0; i < input.Length - 1; i++)
        {
            if (input[i] == _eol[0] && input[i + 1] == _eol[1])
            {
                endOfLastLine = i + 1;
                var startLine = endLine;
                //todo
                //text will include eof. the question with the last line.
                endLine = i + 1;
                LineMemory line = RecognizeMemoryRecord(input[startLine..endLine], startLine);

                records.Add(line);
                lineIndex++;
                i++;
            }
        }
        return ExtractionResult.Ok(lineIndex, endOfLastLine + 1);
    }
    private LineMemory RecognizeMemoryRecord(ReadOnlySpan<byte> lineSpan, int startIndex)
    {
        Span<char> chars = stackalloc char[20];
        for (int i = 0; i < lineSpan.Length - 1; i++)
        {
            if (lineSpan[i] == _lineDelimiter[0] && lineSpan[i + 1] == _lineDelimiter[1])
            {
                for (int j = 0; j < i; j++)
                {
                    //todo encoding
                    chars[j] = (char)lineSpan[j];
                }

                bool success = long.TryParse(chars, out var number);

                //todo !success
                //todo check last index ????
                //text will include ". "
                return new LineMemory(number, startIndex + i, startIndex + lineSpan.Length + 1);
            }
        }

        _error.Writer.TryWrite($"wrong line {ByteToStringConverter.Convert(lineSpan)}");
        //todo
        throw new InvalidOperationException($"wrong line {ByteToStringConverter.Convert(lineSpan)}");

    }
      
    public async ValueTask OnNextAsync(ReadingPhasePackage package)
    {
        await ExtractNext(package);
    }

    public ValueTask OnErrorAsync(Exception error)
    {
        return _readyForNextChunkSubject.OnCompletedAsync();
    }

    public ValueTask OnCompletedAsync()
    {
        //we will complete this sequence as well, in such case there is nothing to do. Something went wrong
        return _readyForNextChunkSubject.OnCompletedAsync();
    }
    private async ValueTask Log(string message)
    {
        //in the real projects it will be structured logs
        string prefix = $"Class: {this.GetType()}, at: {DateTime.UtcNow:hh:mm:ss-fff} ";
        await _logger.LogAsync(prefix + message);
    }
}