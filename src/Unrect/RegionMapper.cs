using System;
using Unrect.Core;

namespace Unrect
{
  public class RegionMapper<TSpace, TRegion, TResult> : IRegionMapper<TSpace, TRegion, TResult>
    where TRegion : IRegion<TSpace>
  {
    public RegionMapper(Func<TRegion, TResult> regionMap)
    {
      RegionMap = regionMap;
    }

    private Func<TRegion, TResult> RegionMap { get; }

    public TResult Map(TRegion space) => RegionMap(space);
  }
}
