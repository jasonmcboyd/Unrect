using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public static class RegionExtensions
  {
    public static IEnumerable<IList<T>> Rows<T> (this IRegion<T> region)
    {
      for (uint i = 0; i < region.Space.Size.Height; i++)
      {
        var result = new List<T>((int)region.Space.Size.Width);

        for (uint j = 0; j < region.Space.Size.Width; j++)
          result.Add(region.Space[(int)j, (int)i]);

        yield return result;
      }
    }

    public static IEnumerable<IList<T>> Columns<T> (this IRegion<T> region)
    {
      for (uint i = 0; i < region.Space.Size.Width; i++)
      {
        var result = new List<T>((int)region.Space.Size.Height);

        for (uint j = 0; j < region.Space.Size.Height; j++)
          result.Add(region.Space[(int)i, (int)j]);

        yield return result;
      }
    }
  }
}
