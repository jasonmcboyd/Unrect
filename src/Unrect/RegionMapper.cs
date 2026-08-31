using System;
using Unrect.Core;

namespace Unrect
{
  public class RegionMapper<TRegion, TResult> : IRegionMapper<TRegion, TResult>
    where TRegion : IRegion
  {
    public RegionMapper(Func<TRegion, TResult> regionMap)
    {
      RegionMap = regionMap;
    }

    private Func<TRegion, TResult> RegionMap { get; }

    public TResult Map(TRegion space) => RegionMap(space);
  }
}
