using System;
using Unrect.Core;

namespace Unrect
{
  public static class RegionMapperFactory
  {
    public static TResult Map<TSpace, TResult>(Region<TSpace> region, Func<Region<TSpace>, TResult> map) => map(region);

    public static IRegionMapper<TSpace, Region1<TSpace, T1>, TResult> Map<TSpace, T1, T1R, TResult>(
      this RegionBuilder1<TSpace, T1> regionBuilder,
      Func<T1, T1R> subregion1Map,
      Func<T1R, TResult> regionMap)
      where T1 : IRegion<TSpace>
    {
      return new RegionMapper<TSpace, Region1<TSpace, T1>, TResult>(region => regionMap(subregion1Map(region.Subregion1)));
    }

    public static IRegionMapper<TSpace, Region1<TSpace, T1>, TResult> Map<TSpace, T1, T1R, TResult>(
      this Region1<TSpace, T1> region,
      Func<T1, T1R> subregion1Map,
      Func<T1R, TResult> regionMap)
      where T1 : IRegion<TSpace>
    {
      return new RegionMapper<TSpace, Region1<TSpace, T1>, TResult>(region => regionMap(subregion1Map(region.Subregion1)));
    }

    public static IRegionMapper<TSpace, Region2<TSpace, T1, T2>, TResult> Map<TSpace, T1, T1R, T2, T2R, TResult>(
      this StackRegionBuilder2<TSpace, T1, T2> regionBuilder,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T1R, T2R, TResult> regionMap)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
    {
      return new RegionMapper<TSpace, Region2<TSpace, T1, T2>, TResult>(region => regionMap(subregion1Map(region.Subregion1), subregion2Map(region.Subregion2)));
    }

    public static IRegionMapper<TSpace, Region2<TSpace, T1, T2>, TResult> Map<TSpace, T1, T1R, T2, T2R, TResult>(
      this Region2<TSpace, T1, T2> region,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T1R, T2R, TResult> regionMap)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
    {
      return new RegionMapper<TSpace, Region2<TSpace, T1, T2>, TResult>(region => regionMap(subregion1Map(region.Subregion1), subregion2Map(region.Subregion2)));
    }

    public static IRegionMapper<TSpace, Region3<TSpace, T1, T2, T3>, TResult> Map<TSpace, T1, T1R, T2, T2R, T3, T3R, TResult>(
      this StackRegionBuilder3<TSpace, T1, T2, T3> regionBuilder,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T3, T3R> subregion3Map,
      Func<T1R, T2R, T3R, TResult> regionMap)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
    {
      return new RegionMapper<TSpace, Region3<TSpace, T1, T2, T3>, TResult>(region =>
        regionMap(
          subregion1Map(region.Subregion1),
          subregion2Map(region.Subregion2),
          subregion3Map(region.Subregion3)));
    }

    //public static IRegionMapper<TSpace, Region3<TSpace, T1, T2, T3>, TResult> Map<TSpace, T1, T1R, T2, T2R, T3, T3R, TResult>(
    //  this StackRegionBuilder3<TSpace, T1, T2, T3> regionBuilder,
    //  Func<T1, T1R> subregion1Map,
    //  Func<T2, T2R> subregion2Map,
    //  Func<T3, T3R> subregion3Map,
    //  Func<T1R, T2R, T3R, TResult> regionMap)
    //  where T1 : IRegion<TSpace>
    //  where T2 : IRegion<TSpace>
    //  where T3 : IRegion<TSpace>
    //{
    //  return new RegionMapper<TSpace, Region3<TSpace, T1, T2, T3>, TResult>(region =>
    //    regionMap(
    //      subregion1Map(region.Subregion1),
    //      subregion2Map(region.Subregion2),
    //      subregion3Map(region.Subregion3)));
    //}

    public static TResult Map<TSpace, T1, T1R, TResult>(
      Region1<TSpace, T1> region,
      Func<Region1<TSpace, T1>, T1R, TResult> map,
      Func<T1, T1R> subregionMap)
      where T1 : IRegion<TSpace>
    {
      return map(region, subregionMap(region.Subregion1));
    }

    public static TResult Map<TSpace, T1, T2, T1R, T2R, TResult>(
      Region2<TSpace, T1, T2> region,
      Func<Region2<TSpace, T1, T2>, T1R, T2R, TResult> map,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
    {
      return map(region, subregion1Map(region.Subregion1), subregion2Map(region.Subregion2));
    }

    public static TResult Map<TSpace, T1, T2, T3, T1R, T2R, T3R, TResult>(
      Region3<TSpace, T1, T2, T3> region,
      Func<Region3<TSpace, T1, T2, T3>, T1R, T2R, T3R, TResult> map,
      Func<T1, T1R> subregion1Map,
      Func<T2, T2R> subregion2Map,
      Func<T3, T3R> subregion3Map)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
    {
      return map(region, subregion1Map(region.Subregion1), subregion2Map(region.Subregion2), subregion3Map(region.Subregion3));
    }
  }
}
