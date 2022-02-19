namespace Unrect.Core
{
  public interface IOffsetStrategy<in TSpace>
  {
    Offset GetOffset(ISpace<TSpace> availableSpace);
  }
}
