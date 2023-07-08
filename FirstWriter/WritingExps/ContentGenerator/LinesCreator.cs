using System.Text;
using Infrastructure;

namespace ContentGenerator;

public class LinesCreator:IDisposable
{
   private readonly int _maxTextLength;
   private readonly int _maxNumberLength;
   public int MaxTextByte { get; init; }
   public int MinTextByte { get; init; }
   public int MaxNumberByte { get; init; }
   public int MinNumberByte { get; init; }
   private readonly Encoding _encoding;
   private LongToBytesConverter _converter;

   readonly byte[] _newLine;
   private readonly byte[] _delimiter;

   public LinesCreator()
   {
      _encoding = Encoding.UTF8;
      _maxNumberLength = ConstValues.MaxNumberLength;
      _maxTextLength = ConstValues.MaxTextLength;
      MinNumberByte = 48;
      MaxNumberByte = 57;
      MinTextByte = 65; // 32;
      MaxTextByte = 126;
      _newLine = _encoding.GetBytes(Environment.NewLine);
      _delimiter = _encoding.GetBytes(". ");
      _converter = new LongToBytesConverter();
   }

   //todo what's new
   private readonly Random _randomNumbers = new(1);
   private readonly Random _randomText = new(2);
   public int NextLine(byte[] line)
   {
      int numberLength = _randomNumbers.Next(1, _maxNumberLength);


      for (int i = 0; i < numberLength; i++)
      {
         //Next NextInt64?
         line[i] = (byte)_randomNumbers.Next(MinNumberByte, MaxNumberByte);
      }
      line[numberLength] = _delimiter[0];
      line[numberLength + 1] = _delimiter[1];

      int textLength = _randomNumbers.Next(1, _maxTextLength);

      for (int i = numberLength + 2; i < numberLength + 2 + textLength; i++)
      {
         line[i] = (byte)_randomText.Next(MinTextByte, MaxTextByte);
      }
      line[numberLength + 2 + textLength] = _newLine[0];
      line[numberLength + 3 + textLength] = _newLine[1];

      return numberLength + textLength + 4;
   }

   public int NextLine(byte[] line, int start)
   {
      int numberLength = _randomNumbers.Next(1, _maxNumberLength);

      for (int i = 0; i < numberLength; i++)
      {
         //Next NextInt64?
         line[start+i] = (byte)_randomNumbers.Next(MinNumberByte, MaxNumberByte);
      }
      line[start + numberLength] = _delimiter[0];
      line[start + numberLength + 1] = _delimiter[1];

      int textLength = _randomNumbers.Next(1, _maxTextLength);

      for (int i = numberLength + 2; i < numberLength + 2 + textLength; i++)
      {
         line[start + i] = (byte)_randomText.Next(MinTextByte, MaxTextByte);
      }
      line[start + numberLength + 2 + textLength] = _newLine[0];
      line[start + numberLength + 3 + textLength] = _newLine[1];

      return numberLength + textLength + 4;
   }

   public int NextLine(Span<byte> line)
   {
      long number = _randomNumbers.NextInt64(0, Int64.MaxValue);
      var (bytes, numberLength) = _converter.ConvertLongToBytes(number);
      for (int i = 0; i < numberLength; i++)
      {
         //Next NextInt64?
         line[i] = bytes.Span[i];
      }

      line[numberLength] = _delimiter[0];
      line[numberLength + 1] = _delimiter[1];

      int textLength = _randomNumbers.Next(1, _maxTextLength) + 2;
      int textStart = numberLength + 2;
      for (int i = textStart; i < textStart + textLength; i++)
      {
         line[i] = (byte)_randomText.Next(MinTextByte, MaxTextByte);
      }

      line[textStart + textLength] = _newLine[0];
      line[textStart + textLength + 1] = _newLine[1];

      return textStart + textLength + 2;
   }

   public void Dispose()
   {
      _converter.Dispose();
   }
}