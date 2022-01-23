﻿using System;
using Unrect.Core;

namespace Unrect.Size
{
  public static class SizeStrategies
  {
    public static ISizeStrategy<TSpace> MaxSize<TSpace>()
      => new MaxSizeStrategy<TSpace>();

    public static ISizeStrategy<TSpace> MinSize<TSpace>()
      => new ExplicitSizeStrategy<TSpace>(0, 0);

    public static ISizeStrategy<TSpace> ExplicitSize<TSpace>(uint width, uint height)
      => new ExplicitSizeStrategy<TSpace>(width, height);

    public static ISizeStrategy<TSpace> SelectSize<TSpace>(Func<ISpace<TSpace>, Core.Size> selector)
      => new SelectorSizeStrategy<TSpace>(selector);
  }
}
