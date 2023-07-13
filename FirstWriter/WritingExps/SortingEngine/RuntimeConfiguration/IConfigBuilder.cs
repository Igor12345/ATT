using System.Text;

namespace SortingEngine.RuntimeConfiguration;

public interface IConfigBuilder
{
   IConfigBuilder UseInputBuffer(int inputBufferSize);
   IConfigBuilder UseFolder(string sourceFile, string folderForChunks);
   IConfigBuilder UseMergeBuffer(int mergeBuffer);
   IConfigBuilder UseOutputBuffer(int outputBuffer);
   IConfigBuilder UseRecordsBuffer(int recordsBuffer);
   IConfigBuilder UseEncoding(Encoding encoding);
   IConfigBuilder UseOutputPath(string outputPath);
}