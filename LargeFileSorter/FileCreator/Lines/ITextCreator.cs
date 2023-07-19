namespace FileCreator.Lines;

public interface ITextCreator
{
    PositionedBuffer WriteText(PositionedBuffer buffer);
}