using System;
using Unrect.Core;
using Unrect.Size;
using static Unrect.Size.SizeStrategies;

namespace Unrect.Area
{
  public static class AreaStrategies<TSpace>
  {
    public static IAreaStrategy<TSpace> MaxArea()
      => MaxSize<TSpace>().ToAreaStrategy();

    public static IAreaStrategy<TSpace> MinArea()
      => MinSize<TSpace>().ToAreaStrategy();

    public static IAreaStrategy<TSpace> ExplicitArea(uint width, uint height)
      => ExplicitSize<TSpace>(width, height).ToAreaStrategy();

    public static IAreaStrategy<TSpace> SelectArea(Func<ISpace<TSpace>, Core.Size> selector)
      => SelectSize(selector).ToAreaStrategy();
  }

  internal static class AreaStrategies
  {
    public static IAreaStrategy<TSpace> MaxArea<TSpace>() => AreaStrategies<TSpace>.MaxArea();

    public static IAreaStrategy<TSpace> MinArea<TSpace>() => AreaStrategies<TSpace>.MinArea();

    public static IAreaStrategy<TSpace> ExplicitArea<TSpace>(uint width, uint height)
      => AreaStrategies<TSpace>.ExplicitArea(width, height);

    public static IAreaStrategy<TSpace> SelectArea<TSpace>(Func<ISpace<TSpace>, Core.Size> selector)
      => AreaStrategies<TSpace>.SelectArea(selector);
  }
}
