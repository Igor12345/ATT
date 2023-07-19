using BenchmarkDotNet.Running;
using SimpleBenchmarks;
using SimpleBenchmarks.ByteOperations;
using SimpleBenchmarks.IOOperations;

// var summaryComparer = BenchmarkRunner.Run<StringsComparisonBenchmark>();
// var arrayVsSpan = BenchmarkRunner.Run<ArrayVsSpan>();
// var longToBytesAlgorithmsComparison = BenchmarkRunner.Run<NumberToBytesConversion>();
var syncVsAsyncOperations = BenchmarkRunner.Run<SyncVsAsyncOperations>();