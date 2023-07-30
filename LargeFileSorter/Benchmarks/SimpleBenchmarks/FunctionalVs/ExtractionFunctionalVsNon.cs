using System.Text;
using BenchmarkDotNet.Attributes;
using SortingEngine.Algorithms;
using SortingEngine.DataStructures;
using SortingEngine.Entities;
using SortingEngine.RowData;
using SortingEngineTests.TestUtils;

namespace SimpleBenchmarks.FunctionalVs;

[MemoryDiagnoser]
public class ExtractionFunctionalVsNon
{
    
    [Params(1_000, 10_000)]
    public int N;

    private readonly List<string> _samples = new()
    {
        "12345. apple",
        "67890. orange"
    };

    private string[]? _lines;
    private byte[]? _input;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        _lines = new string[N]; 
            
        for (int i = 0; i < N; i++)
        {
            _lines[i] = _samples[i % 2];
        }
        _input = DataGenerator.Use(Encoding.UTF8).CreateWholeBytes(
            _lines,
            DataGenerator.RandomBytes(12));
    }

    [Benchmark]
    public int ExtractNonFunctional()
    {
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(1000);

        Encoding encoding = Encoding.UTF8;
        LineParser parser = new LineParser(KmpMatcher.CreateForThisPattern(encoding.GetBytes(TestConstants.Delimiter)),
            encoding);
        IParticularSubstringMatcher eolFinder = KmpMatcher.CreateForThisPattern(encoding.GetBytes(Environment.NewLine));
        var extractor = new LinesExtractor( eolFinder, encoding.GetBytes(Environment.NewLine).Length, parser);

        var result = extractor.ExtractRecords(_input.AsSpan(), linesStorage);
        return result.LinesNumber;
    }

    [Benchmark]
    public int ExtractFunctional()
    {
        ExpandingStorage<Line> linesStorage = new ExpandingStorage<Line>(1000);

        Encoding encoding = Encoding.UTF8;
        var parser = new SortingEngine.RowDataFunctional.LineParser(KmpMatcher.CreateForThisPattern(encoding.GetBytes(TestConstants.Delimiter)),
            encoding);
        var eolFinder = SortingEngine.Algorithms.OnMemory.KmpMatcher.CreateForThisPattern(encoding.GetBytes(Environment.NewLine), true);
        var extractor = new SortingEngine.RowDataFunctional.LinesExtractor( eolFinder, parser);

        var result = extractor.ExtractRecords(_input.AsMemory(), linesStorage);
        return result.Case as int? ?? 0;
    }
}