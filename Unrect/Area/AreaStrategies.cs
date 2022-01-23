using System;
using Unrect.Core;
using Unrect.Size;

using static Unrect.Size.SizeStrategies;

namespace Unrect.Area
{
  public static class AreaStrategies<TSpace>
  {
    public static Core.IAreaStrategy<TSpace> MaxArea()
      => MaxSize<TSpace>().ToAreaStrategy();

    public static Core.IAreaStrategy<TSpace> MinArea()
      => MinSize<TSpace>().ToAreaStrategy();

    public static Core.IAreaStrategy<TSpace> ExplicitArea(uint width, uint height)
      => ExplicitSize<TSpace>(width, height).ToAreaStrategy();

    public static Core.IAreaStrategy<TSpace> SelectArea(Func<ISpace<TSpace>, Core.Size> selector)
      => SelectSize<TSpace>(selector).ToAreaStrategy();
  }
}
