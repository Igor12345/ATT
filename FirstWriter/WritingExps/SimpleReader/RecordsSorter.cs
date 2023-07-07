namespace SimpleReader
{
   public class RecordsSorter
   {
      public LineRecord[] Sort(LineRecord[] input)
      {
         return input.Order(new RecordsComparer()).ToArray();
      }
   }
}
