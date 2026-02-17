using System;
using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public static class RegionExtensions
  {
    public static IEnumerable<IList<T>> Rows<T> (this IRegion<T> region)
    {
      for (int i = 0; i < region.Space.Area.Size.Height; i++)
      {
        var result = new List<T>(region.Space.Area.Size.Width);

        for (int j = 0; j < region.Space.Area.Size.Width; j++)
          result.Add(region.Space[j, i]);

        yield return result;
      }
    }

    public static IEnumerable<IList<T>> Columns<T> (this IRegion<T> region)
    {
      for (int i = 0; i < region.Space.Area.Size.Width; i++)
      {
        var result = new List<T>(region.Space.Area.Size.Height);

        for (int j = 0; j < region.Space.Area.Size.Height; j++)
          result.Add(region.Space[i, j]);

        yield return result;
      }
    }

    public static IEnumerable<T> RowOrderEnumerable<T>(this IRegion<T> region)
    {
      for (int i = 0; i < region.Space.Area.Size.Height; i++)
        for (int j = 0; j < region.Space.Area.Size.Width; j++)
          yield return region.Space[j, i];
    }

    public static IEnumerable<T> ColumnOrderEnumerable<T> (this IRegion<T> region)
    {
      for (int i = 0; i < region.Space.Area.Size.Width; i++)
        for (int j = 0; j < region.Space.Area.Size.Height; j++)
          yield return region.Space[i, j];
    }

    public static T[,] ToArray<T>(this IRegion<T> region)
    {
      var result = new T[region.Space.Area.Size.Height, region.Space.Area.Size.Width];

      for (int i = 0; i < region.Space.Area.Size.Height; i++)
        for (int j = 0; j < region.Space.Area.Size.Width; j++)
          result[i, j] = region.Space[j, i];

      return result;
    }

    public static TResult Map<TSpace, T1, TResult>(
      this Region1<TSpace, T1> region,
      Func<T1, Region1<TSpace, T1>, TResult> map)
      where T1 : IRegion<TSpace>
      => map(region.Subregion1, region);

    public static TResult Map<TSpace, T1, T2, TResult>(
      this Region2<TSpace, T1, T2> region,
      Func<T1, T2, Region2<TSpace, T1, T2>, TResult> map)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => map(region.Subregion1, region.Subregion2, region);

    public static TResult Map<TSpace, T1, T2, T3, TResult>(
      this Region3<TSpace, T1, T2, T3> region,
      Func<T1, T2, T3, Region3<TSpace, T1, T2, T3>, TResult> map)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => map(region.Subregion1, region.Subregion2, region.Subregion3, region);
  }
}
