﻿using System;
using Unrect.Core;
using Unrect.Size;
using static Unrect.Size.SizeStrategies;

namespace Unrect.Offset
{
  public static class OffsetStrategies<TSpace>
  {
    public static IOffsetStrategy<TSpace> MaxOffset()
      => MaxSize<TSpace>().ToOffsetStrategy();

    public static IOffsetStrategy<TSpace> MinOffset()
      => MinSize<TSpace>().ToOffsetStrategy();

    public static IOffsetStrategy<TSpace> ExplicitOffset(uint width, uint height)
      => ExplicitSize<TSpace>(width, height).ToOffsetStrategy();

    public static IOffsetStrategy<TSpace> SelectOffset(Func<ISpace<TSpace>, Core.Size> selector)
      => SelectSize(selector).ToOffsetStrategy();
  }

  internal static class OffsetStrategies
  {
    public static IOffsetStrategy<TSpace> MaxOffset<TSpace>() => OffsetStrategies<TSpace>.MaxOffset();

    public static IOffsetStrategy<TSpace> MinOffset<TSpace>() => OffsetStrategies<TSpace>.MinOffset();

    public static IOffsetStrategy<TSpace> ExplicitOffset<TSpace>(uint width, uint height)
      => OffsetStrategies<TSpace>.ExplicitOffset(width, height);

    public static IOffsetStrategy<TSpace> SelectOffset<TSpace>(Func<ISpace<TSpace>, Core.Size> selector)
      => OffsetStrategies<TSpace>.SelectOffset(selector);
  }
}
