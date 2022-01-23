namespace Unrect.Core
{
  public interface ISizeStrategy<in TSpace, out TSize>
    where TSize : Size
  {
    TSize GetSize(ISpace<TSpace> availableSpace);
  }
}
