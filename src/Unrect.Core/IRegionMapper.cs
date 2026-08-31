namespace Unrect.Core
{
  public interface IRegionMapper<in TRegion, out TResult>
    where TRegion : IRegion
  {
    TResult Map(TRegion space);
  }
}
