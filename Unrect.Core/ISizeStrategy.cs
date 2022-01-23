namespace Unrect.Core
{
  public interface ISizeStrategy<in TSpace>
  {
    Size GetSize(ISpace<TSpace> availableSpace);
  }
}
