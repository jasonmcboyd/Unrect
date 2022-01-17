using Unrect.Core;
using Unrect.OffsetStrategies;
using Unrect.SizeStrategies;
using static Unrect.OffsetStrategy;
using static Unrect.SizeStrategy;

namespace Unrect
{
  public static class RegionBuilderFactory<TSpace>
  {
    public static RegionBuilder<TSpace> Builder(
      IOffsetStrategy offsetStrategy,
      ISizeStrategy sizeStrategy)
    {
      return new RegionBuilder<TSpace>()
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy,
      };
    }
    public static RegionBuilder<TSpace> Builder(IOffsetStrategy offsetStrategy) => Builder(offsetStrategy, Max());
    public static RegionBuilder<TSpace> Builder(ISizeStrategy sizeStrategy) => Builder(None(), sizeStrategy);
    public static RegionBuilder<TSpace> Builder() => Builder(None(), Max());
    public static RegionBuilder<TSpace> Builder(uint leftOffset, uint topOffset, uint width, uint height)
      => Builder(new ExplicitOffsetStrategy(leftOffset, topOffset), new ExplicitSizeStrategy(width, height));
    public static RegionBuilder<TSpace> Builder(uint leftOffset, uint topOffset, ISizeStrategy sizeStrategy)
      => Builder(new ExplicitOffsetStrategy(leftOffset, topOffset), sizeStrategy);
    public static RegionBuilder<TSpace> Builder(uint width, uint height, IOffsetStrategy offsetStrategy)
      => Builder(offsetStrategy, new ExplicitSizeStrategy(width, height));

    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      IOffsetStrategy offsetStrategy,
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
    {
      return new RegionBuilder1<TSpace, T1>(subregionBuilder1)
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy
      };
    }
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(offsetStrategy, Max(), subregionBuilder1);
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(None(), sizeStrategy, subregionBuilder1);
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(None(), Max(), subregionBuilder1);
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(new ExplicitOffsetStrategy(leftOffset, topOffset), new ExplicitSizeStrategy(width, height), subregionBuilder1);

    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      IOffsetStrategy offsetStrategy,
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
    {
      return new StackRegionBuilder2<TSpace, T1, T2>(subregionBuilder1, subregionBuilder2)
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy,
        Orientation = Orientation.Horizontal
      };
    }
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(offsetStrategy, Max(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(None(), sizeStrategy, subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(None(), Max(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(new ExplicitOffsetStrategy(leftOffset, topOffset), new ExplicitSizeStrategy(width, height), subregionBuilder1, subregionBuilder2);

    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      IOffsetStrategy offsetStrategy,
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
    {
      return new StackRegionBuilder2<TSpace, T1, T2>(subregionBuilder1, subregionBuilder2)
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy
      };
    }
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(offsetStrategy, Max(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(None(), sizeStrategy, subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(None(), Max(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(new ExplicitOffsetStrategy(leftOffset, topOffset), new ExplicitSizeStrategy(width, height), subregionBuilder1, subregionBuilder2);

    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
    {
      return new StackRegionBuilder3<TSpace, T1, T2, T3>(subregionBuilder1, subregionBuilder2, subregionBuilder3)
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy,
        Orientation = Orientation.Horizontal
      };
    }
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(offsetStrategy, Max(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(None(), sizeStrategy, subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(None(), Max(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(new ExplicitOffsetStrategy(leftOffset, topOffset), new ExplicitSizeStrategy(width, height), subregionBuilder1, subregionBuilder2, subregionBuilder3);

    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
    {
      return new StackRegionBuilder3<TSpace, T1, T2, T3>(subregionBuilder1, subregionBuilder2, subregionBuilder3)
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy
      };
    }
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(offsetStrategy, Max(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      ISizeStrategy sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(None(), sizeStrategy, subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(None(), Max(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(new ExplicitOffsetStrategy(leftOffset, topOffset), new ExplicitSizeStrategy(width, height), subregionBuilder1, subregionBuilder2, subregionBuilder3);
  }
}

