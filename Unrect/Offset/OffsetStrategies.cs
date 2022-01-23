using System;
using Unrect.Core;
using Unrect.Size;

using static Unrect.Size.SizeStrategies;

namespace Unrect.Offset
{
  public static class OffsetStrategies<TSpace>
  {
    public static Core.IOffsetStrategy<TSpace> MaxOffset()
      => MaxSize<TSpace>().ToOffsetStrategy();

    public static Core.IOffsetStrategy<TSpace> MinOffset()
      => MinSize<TSpace>().ToOffsetStrategy();

    public static Core.IOffsetStrategy<TSpace> ExplicitOffset(uint width, uint height)
      => ExplicitSize<TSpace>(width, height).ToOffsetStrategy();

    public static Core.IOffsetStrategy<TSpace> SelectOffset(Func<ISpace<TSpace>, Core.Size> selector)
      => SelectSize<TSpace>(selector).ToOffsetStrategy();
  }
}
