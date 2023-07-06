// See https://aka.ms/new-console-template for more information

using System.Text;
using ContentGenerator;
using SimpleWriter;

string str ="Hello, World!";

// byte[] utf16Bytes = Encoding.Unicode.GetBytes(str);
// byte[] utf8Bytes = Encoding.UTF8.GetBytes(str);
// byte[] utf32Bytes = Encoding.UTF32.GetBytes(str);
//
// foreach (char c in str)
// {
//    byte b = (byte)c;
//    Console.WriteLine($"char - {c}; byte - {b}");
// }
//
// Console.WriteLine($"char - {'a'}; byte - {(byte)'a'}");
// Console.WriteLine($"char - {'z'}; byte - {(byte)'z'}");
// Console.WriteLine($"char - {'A'}; byte - {(byte)'A'}");
// Console.WriteLine($"char - {'Z'}; byte - {(byte)'Z'}");
// Console.WriteLine($"char - {'0'}; byte - {(byte)'0'}");
// Console.WriteLine($"char - {'9'}; byte - {(byte)'9'}");
// Console.WriteLine($"char - {' '}; byte - {(byte)' '}");
// Console.WriteLine($"char - {'.'}; byte - {(byte)'.'}");

// var numberCreator = TextCreator.NumbersCreator;
//
// foreach (byte[] bytes in numberCreator.GenerateBytes())
// {
//    var strNum = Encoding.ASCII.GetString(bytes);
//    Console.WriteLine(strNum);
//
// }
//
// Console.ReadLine();
Console.WriteLine("-----------------------------------");
TextCreator creator = new TextCreator(30, Encoding.Default, true);
ITextCreator utf8 = new TextConvertor(creator, Encoding.Default);
ITextCreator ascii = new TextConvertor(creator, Encoding.ASCII);
ITextCreator utf16 = new TextConvertor(creator, Encoding.Unicode);
ITextCreator utf32 = new TextConvertor(creator, Encoding.UTF32);
// foreach (byte[] bytes in creator.GenerateBytes())
// {
//    foreach (byte b in bytes)
//    {
//       Console.WriteLine($"byte: {b}, char: {(char)b}");
//    }
//    break;
// }
// Console.ReadLine();

Console.WriteLine("Creating first big file");

string fileName = "first";
long lines = 10_000_000;
await using(InfoLogger logger = new InfoLogger(fileName, $"{Encoding.UTF8.BodyName} - {lines}"))
{
   FirstExecutor firstExecutor = new FirstExecutor(fileName, lines);
   await firstExecutor.CreateFile();
}

Console.WriteLine("________________________________________________________________________________");

Console.WriteLine("Creating second big file");
fileName = "second";
await using (InfoLogger logger = new InfoLogger(fileName, $"{Encoding.UTF8.BodyName} - {lines}"))
{
   SecondExecutor secondExecutor = new SecondExecutor(fileName, lines);
   await secondExecutor.CreateFile();
}

Console.WriteLine("________________________________________________________________________________");

Console.WriteLine("Creating third big file");
fileName = "third";
await using (InfoLogger logger = new InfoLogger(fileName, $"{Encoding.UTF8.BodyName} - {lines}"))
{
   ThirdExecutor thirdExecutor = new ThirdExecutor(fileName, lines);
   await thirdExecutor.CreateFile();
}

Console.WriteLine("________________________________________________________________________________");

Console.WriteLine("Creating fourth big file");
fileName = "fourth";
using (InfoLogger logger = new InfoLogger(fileName, $"{Encoding.UTF8.BodyName} - {lines}"))
{
   FourthExecutor fourthExecutor = new FourthExecutor(fileName, lines);
   fourthExecutor.CreateFile();
}

Console.WriteLine("________________________________________________________________________________");
Console.ReadLine();

FileCreator fileCreator = new FileCreator(creator);
Console.WriteLine("Creating string file");
await fileCreator.CreateStringFileAsync(@"d://temp/utf8_origin.txt", 1000);
Console.WriteLine("String file created");

Console.WriteLine("Creating binary files");
await fileCreator.CreateBinaryFileAsync(@"d://temp/utf8_bytes_origin.txt", 1000);

FileCreator fileCreatorUtf8 = new FileCreator(utf8);
await fileCreatorUtf8.CreateBinaryFileAsync(@"d://temp/utf8_second.txt", 1000);

FileCreator fileCreatorUtf16 = new FileCreator(utf16);
await fileCreatorUtf16.CreateBinaryFileAsync(@"d://temp/utf16_second.txt", 1000);

FileCreator fileCreatorUtf32 = new FileCreator(utf32);
await fileCreatorUtf32.CreateBinaryFileAsync(@"d://temp/utf32_second.txt", 1000);

FileCreator fileCreatorAscii = new FileCreator(ascii);
await fileCreatorAscii.CreateBinaryFileAsync(@"d://temp/ascii_second.txt", 1000);
Console.WriteLine("Binary files created");

Console.ReadLine();