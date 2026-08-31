using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  public static class AreaStrategies
  {
    public static IAreaStrategy MaxArea()
      => MaxSize().ToAreaStrategy();

    public static IAreaStrategy MinArea()
      => MinSize().ToAreaStrategy();

    public static IAreaStrategy ExplicitArea(int width, int height)
      => ExplicitSize(width, height).ToAreaStrategy();

    public static IAreaStrategy SelectArea(Func<ISpace, Size> selector)
      => SelectSize(selector).ToAreaStrategy();
  }
}
