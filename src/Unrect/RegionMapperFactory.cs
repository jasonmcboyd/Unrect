using System;
using Unrect.Core;

namespace Unrect
{
  public static class RegionMapperFactory
  {
    public static TResult Map<TResult>(Region region, Func<Region, TResult> map) => map(region);

    public static IRegionMapper<Region1<T1>, TResult> Map<T1, T1R, TResult>(
      this RegionBuilder1<T1> regionBuilder,
      Func<T1, T1R> subregion1Map,
      Func<T1R, TResult> regionMap)
      where T1 : IRegion
    {
      return new RegionMapper<Region1<T1>, TResult>(region => regionMap(subregion1Map(region.Subregion1)));
    }

    public static IRegionMapper<Region1<T1>, TResult> Map<T1, T1R, TResult>(
      this Region1<T1> region,
      Func<T1, T1R> subregion1Map,
      Func<T1R, TResult> regionMap)
      where T1 : IRegion
    {
      return new RegionMapper<Region1<T1>, TResult>(region => regionMap(subregion1Map(region.Subregion1)));
    }

    public static IRegionMapper<Region2<T1, T2>, TResult> Map<T1, T1R, T2, T2R, TResult>(
      this StackRegionBuilder2<T1, T2> regionBuilder,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T1R, T2R, TResult> regionMap)
      where T1 : IRegion
      where T2 : IRegion
    {
      return new RegionMapper<Region2<T1, T2>, TResult>(region => regionMap(subregion1Map(region.Subregion1), subregion2Map(region.Subregion2)));
    }

    public static IRegionMapper<Region2<T1, T2>, TResult> Map<T1, T1R, T2, T2R, TResult>(
      this Region2<T1, T2> region,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T1R, T2R, TResult> regionMap)
      where T1 : IRegion
      where T2 : IRegion
    {
      return new RegionMapper<Region2<T1, T2>, TResult>(region => regionMap(subregion1Map(region.Subregion1), subregion2Map(region.Subregion2)));
    }

    public static IRegionMapper<Region3<T1, T2, T3>, TResult> Map<T1, T1R, T2, T2R, T3, T3R, TResult>(
      this StackRegionBuilder3<T1, T2, T3> regionBuilder,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T3, T3R> subregion3Map,
      Func<T1R, T2R, T3R, TResult> regionMap)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
    {
      return new RegionMapper<Region3<T1, T2, T3>, TResult>(region =>
        regionMap(
          subregion1Map(region.Subregion1),
          subregion2Map(region.Subregion2),
          subregion3Map(region.Subregion3)));
    }

    public static TResult Map<T1, T1R, TResult>(
      Region1<T1> region,
      Func<Region1<T1>, T1R, TResult> map,
      Func<T1, T1R> subregionMap)
      where T1 : IRegion
    {
      return map(region, subregionMap(region.Subregion1));
    }

    public static TResult Map<T1, T2, T1R, T2R, TResult>(
      Region2<T1, T2> region,
      Func<Region2<T1, T2>, T1R, T2R, TResult> map,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map)
      where T1 : IRegion
      where T2 : IRegion
    {
      return map(region, subregion1Map(region.Subregion1), subregion2Map(region.Subregion2));
    }

    public static TResult Map<T1, T2, T3, T1R, T2R, T3R, TResult>(
      Region3<T1, T2, T3> region,
      Func<Region3<T1, T2, T3>, T1R, T2R, T3R, TResult> map,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T3, T3R> subregion3Map)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
    {
      return map(region, subregion1Map(region.Subregion1), subregion2Map(region.Subregion2), subregion3Map(region.Subregion3));
    }
  }
}
