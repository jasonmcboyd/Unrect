using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  public static class OffsetStrategies<TSpace>
  {
    public static IOffsetStrategy<TSpace> MaxOffset()
      => MaxSize<TSpace>().ToOffsetStrategy();

    public static IOffsetStrategy<TSpace> MinOffset()
      => MinSize<TSpace>().ToOffsetStrategy();

    public static IOffsetStrategy<TSpace> Offset(uint width, uint height)
      => Size<TSpace>(width, height).ToOffsetStrategy();

    public static IOffsetStrategy<TSpace> SelectOffset(Func<ISpace<TSpace>, Size> selector)
      => SelectSize(selector).ToOffsetStrategy();
  }

  public static class OffsetStrategies
  {
    public static IOffsetStrategy<TSpace> MaxOffset<TSpace>() => OffsetStrategies<TSpace>.MaxOffset();

    public static IOffsetStrategy<TSpace> MinOffset<TSpace>() => OffsetStrategies<TSpace>.MinOffset();

    public static IOffsetStrategy<TSpace> ExplicitOffset<TSpace>(uint width, uint height)
      => OffsetStrategies<TSpace>.Offset(width, height);

    public static IOffsetStrategy<TSpace> SelectOffset<TSpace>(Func<ISpace<TSpace>, Size> selector)
      => OffsetStrategies<TSpace>.SelectOffset(selector);
  }
}
