// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using SimpleBenchmarks;

// var summary = BenchmarkRunner.Run<RandomBenchmark>();
// var summaryComparer = BenchmarkRunner.Run<StringsComparisonBenchmark>();
var summaryComparer = BenchmarkRunner.Run<ArrayVsSpan>();