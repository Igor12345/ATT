using System.Text;

namespace SortingEngine.RuntimeConfiguration;

internal interface IConfigBuilder
{
   IConfigBuilder UseInputBuffer(int inputBufferSize);
   IConfigBuilder UseFolder(string folderForChunks);
   IConfigBuilder UseMergeBuffer(int mergeBuffer);
   IConfigBuilder UseOutputBuffer(int outputBuffer);
   IConfigBuilder UseRecordsBuffer(int recordsBuffer);
   IConfigBuilder UseEncoding(Encoding encoding);
}