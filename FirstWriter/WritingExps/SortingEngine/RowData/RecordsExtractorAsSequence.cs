using System.Buffers;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Infrastructure.ByteOperations;
using SortingEngine.DataStructures;
using SortingEngine.Entities;

namespace SortingEngine.RowData;

public class RecordsExtractorAsSequence : IAsyncObserver<InputBuffer>
{
    private readonly CancellationToken _token;
    private Channel<int> _output = Channel.CreateBounded<int>(1);
    private Channel<ReadOnlyMemory<byte>> _input = Channel.CreateBounded<ReadOnlyMemory<byte>>(1);
    private Channel<string> _error = Channel.CreateUnbounded<string>();

    readonly SimpleAsyncSubject<byte[]> _readyForNextChunkSubject = new SequentialSimpleAsyncSubject<byte[]>();
    readonly SimpleAsyncSubject<PreSortBuffer> _readyForSortingSubject = new SequentialSimpleAsyncSubject<PreSortBuffer>();
      
    private readonly byte[] _eol;
    private readonly byte[] _lineDelimiter;
    private ReadOnlyMemory<byte> _inputButes;
    private ExpandingStorage<LineMemory> _readyRecords;

    public RecordsExtractorAsSequence(byte[] eol, byte[] lineDelimiter, CancellationToken token)
    {
        _eol = eol;
        _lineDelimiter = lineDelimiter;
        _token = token;
    }

    public IAsyncObservable<byte[]> ReadyForNextChunk => _readyForNextChunkSubject;
    public IAsyncObservable<PreSortBuffer> ReadyForSorting => _readyForSortingSubject;

    private async Task ExtractNext(InputBuffer inputBuffer)
    {
        ReadOnlyMemory<byte> inputBytes = inputBuffer.Buffer.AsMemory()[..inputBuffer.ReadBytes];
        var result = ExtractRecords(inputBytes.Span, _readyRecords);
        if (!result.Success)
        {
            await _readyForNextChunkSubject.OnErrorAsync(new InvalidOperationException(result.Message));
        }
        if (_inputButes.Length - result.StartRemainingBytes > 0)
        {
            var remainedBytes = ArrayPool<byte>.Shared.Rent(Constants.MaxTextLength);
            _inputButes.Span[result.StartRemainingBytes..].CopyTo(remainedBytes);

            //todo
            PreSortBuffer preSortBuffer = new PreSortBuffer()
                { Write = inputBytes, LinesNumber = result.Size, RecordsStorage = _readyRecords };
            
            //todo!!!
            await _readyForSortingSubject.OnNextAsync(preSortBuffer);
            await _readyForNextChunkSubject.OnNextAsync(remainedBytes);
        }
        await _readyForNextChunkSubject.OnNextAsync(Array.Empty<byte>());
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
      
    public async ValueTask OnNextAsync(InputBuffer inputBuffer)
    {
        await ExtractNext(inputBuffer);
    }

    public ValueTask OnErrorAsync(Exception error)
    {
        return _readyForNextChunkSubject.OnCompletedAsync();
    }

    public ValueTask OnCompletedAsync()
    {
        return _readyForNextChunkSubject.OnCompletedAsync();
    }
}