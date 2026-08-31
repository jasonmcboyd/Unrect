namespace Unrect.Core
{
  public interface IOffsetStrategy
  {
    Offset GetOffset(ISpace availableSpace);
  }
}
