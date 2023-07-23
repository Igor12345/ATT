using System.Text;

namespace SortingEngine.RuntimeConfiguration;

//in this case it was a bad idea
public interface IConfigBuilder
{
   IConfigBuilder UseInputBuffer(int inputBufferSize);
   IConfigBuilder UseFileAndFolder(string sourceFile, string folderForChunks);
   IConfigBuilder UseMergeBuffer(int mergeBuffer);
   IConfigBuilder UseOutputBuffer(int outputBuffer);
   IConfigBuilder UseRecordsBuffer(int recordsBuffer);
   IConfigBuilder UseReadStreamBufferSize(int readStreamBufferSize);
   IConfigBuilder UseWriteStreamBufferSize(int writeStreamBufferSize);
   IConfigBuilder SortingPhaseConcurrency(int sortingPhaseConcurrency);
   IConfigBuilder UseOutputPath(string outputPath);
   IConfigBuilder UseDelimiter(byte[] delimiterBytes);
   IConfigBuilder UseEolBytes(byte[] eolBytes);
   IConfigBuilder UseMaxLineLength(int maxLineLength);
   IConfigBuilder UseOneWay(bool useOneStepSorting);
   IConfigBuilder UseKeepReadStreamOpen(bool keepReadStreamOpen);
   IConfigBuilder UseEncoding(Encoding encoding);
}