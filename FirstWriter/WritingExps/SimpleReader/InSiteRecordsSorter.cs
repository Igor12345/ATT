namespace SimpleReader;

public class InSiteRecordsSorter
{
   private readonly ReadOnlyMemory<byte> _source;

   public InSiteRecordsSorter(ReadOnlyMemory<byte> source)
   {
      _source = source;
   }

   public LineMemory[] Sort(LineMemory[] input)
   {
      return input.Order(new InSiteRecordsComparer(_source)).ToArray();
   }
}