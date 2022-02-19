namespace Unrect.Core
{
  public interface IAreaStrategy<in TSpace>
  {
    Area GetArea(ISpace<TSpace> availableSpace);
  }
}
