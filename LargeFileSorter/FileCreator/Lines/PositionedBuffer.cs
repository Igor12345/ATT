namespace FileCreator.Lines;

//in fact, is not safe, the buffer can be used somebody else
public record struct PositionedBuffer(Memory<byte> Buffer, int Position);