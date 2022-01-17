namespace Unrect.Core
{
  public interface IRegionMapper<in TSpace, in TRegion, out TResult>
    where TRegion : IRegion<TSpace>
  {
    TResult Map(TRegion space);
  }
}
