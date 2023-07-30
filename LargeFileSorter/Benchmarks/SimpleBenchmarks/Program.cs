using BenchmarkDotNet.Running;
using SimpleBenchmarks;
using SimpleBenchmarks.ByteOperations;
using SimpleBenchmarks.FunctionalVs;
using SimpleBenchmarks.IOOperations;

// var summaryComparer = BenchmarkRunner.Run<StringsComparisonBenchmark>();
// var arrayVsSpan = BenchmarkRunner.Run<ArrayVsSpan>();
// var longToBytesAlgorithmsComparison = BenchmarkRunner.Run<NumberToBytesConversion>();
// var longToBytesEncodingsComparison = BenchmarkRunner.Run<NumberToBytesConversionDifferentEncodings>();
// var syncVsAsyncOperations = BenchmarkRunner.Run<SyncVsAsyncOperations>();
var extractionFunctionalVsNon = BenchmarkRunner.Run<ExtractionFunctionalVsNon>();