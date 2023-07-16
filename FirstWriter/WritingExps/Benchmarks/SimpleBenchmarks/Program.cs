using BenchmarkDotNet.Running;
using SimpleBenchmarks;
using SimpleBenchmarks.ByteOperations;
using SimpleBenchmarks.IOOperations;

// var summary = BenchmarkRunner.Run<RandomBenchmark>();
// var summaryComparer = BenchmarkRunner.Run<StringsComparisonBenchmark>();
// var summaryComparer = BenchmarkRunner.Run<ArrayVsSpan>();
// var longToBytesAlgorithmsComparison = BenchmarkRunner.Run<NumberToBytesConversion>();
var SyncVsAsyncOperations = BenchmarkRunner.Run<SyncVsAsyncOperations>();