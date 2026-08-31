using System;
using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public static class RegionExtensions
  {
    public static IEnumerable<IList<CellValue>> Rows(this IRegion region)
    {
      for (int i = 0; i < region.Space.Area.Size.Height; i++)
      {
        var result = new List<CellValue>(region.Space.Area.Size.Width);

        for (int j = 0; j < region.Space.Area.Size.Width; j++)
          result.Add(region.Space[j, i]);

        yield return result;
      }
    }

    public static IEnumerable<IList<CellValue>> Columns(this IRegion region)
    {
      for (int i = 0; i < region.Space.Area.Size.Width; i++)
      {
        var result = new List<CellValue>(region.Space.Area.Size.Height);

        for (int j = 0; j < region.Space.Area.Size.Height; j++)
          result.Add(region.Space[i, j]);

        yield return result;
      }
    }

    public static IEnumerable<CellValue> RowOrderEnumerable(this IRegion region)
    {
      for (int i = 0; i < region.Space.Area.Size.Height; i++)
        for (int j = 0; j < region.Space.Area.Size.Width; j++)
          yield return region.Space[j, i];
    }

    public static IEnumerable<CellValue> ColumnOrderEnumerable(this IRegion region)
    {
      for (int i = 0; i < region.Space.Area.Size.Width; i++)
        for (int j = 0; j < region.Space.Area.Size.Height; j++)
          yield return region.Space[i, j];
    }

    public static CellValue[,] ToArray(this IRegion region)
    {
      var result = new CellValue[region.Space.Area.Size.Height, region.Space.Area.Size.Width];

      for (int i = 0; i < region.Space.Area.Size.Height; i++)
        for (int j = 0; j < region.Space.Area.Size.Width; j++)
          result[i, j] = region.Space[j, i];

      return result;
    }

    public static TResult Map<T1, TResult>(
      this Region1<T1> region,
      Func<T1, Region1<T1>, TResult> map)
      where T1 : IRegion
      => map(region.Subregion1, region);

    public static TResult Map<T1, T2, TResult>(
      this Region2<T1, T2> region,
      Func<T1, T2, Region2<T1, T2>, TResult> map)
      where T1 : IRegion
      where T2 : IRegion
      => map(region.Subregion1, region.Subregion2, region);

    public static TResult Map<T1, T2, T3, TResult>(
      this Region3<T1, T2, T3> region,
      Func<T1, T2, T3, Region3<T1, T2, T3>, TResult> map)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => map(region.Subregion1, region.Subregion2, region.Subregion3, region);
  }
}
