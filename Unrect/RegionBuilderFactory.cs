using Unrect.Core;
using Unrect.Offset;
using Unrect.Area;
using Unrect.Size;
using static Unrect.Area.AreaStrategies<>;

namespace Unrect
{
  public static class RegionBuilderFactory<TSpace>
  {
    public static RegionBuilder<TSpace> Builder(
      IOffsetStrategy<TSpace> offsetStrategy,
      IAreaStrategy<TSpace> sizeStrategy)
    {
      return new RegionBuilder<TSpace>()
      {
        OffsetStrategy = offsetStrategy,
        SizeStrategy = sizeStrategy,
      };
    }
    public static RegionBuilder<TSpace> Builder(IOffsetStrategy<TSpace> offsetStrategy) => Builder(offsetStrategy, MaxArea());
    public static RegionBuilder<TSpace> Builder(IAreaStrategy<TSpace> sizeStrategy) => Builder(OffsetStrategies<TSpace>.MinOffset(), sizeStrategy);
    public static RegionBuilder<TSpace> Builder() => Builder(OffsetStrategies<TSpace>.MinOffset(), AreaStrategies<TSpace>.MaxArea());
    public static RegionBuilder<TSpace> Builder(uint leftOffset, uint topOffset, uint width, uint height)
      => Builder(OffsetStrategies<TSpace>.ExplicitOffset(leftOffset, topOffset), AreaStrategies<TSpace>.ExplicitArea(width, height));
    public static RegionBuilder<TSpace> Builder(uint leftOffset, uint topOffset, IAreaStrategy<TSpace> sizeStrategy)
      => Builder(OffsetStrategies<TSpace>.ExplicitOffset(leftOffset, topOffset), sizeStrategy);
    public static RegionBuilder<TSpace> Builder(uint width, uint height, IOffsetStrategy<TSpace> offsetStrategy)
      => Builder(offsetStrategy, AreaStrategies<TSpace>.ExplicitArea(width, height));

    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      IOffsetStrategy<TSpace> offsetStrategy,
      IAreaStrategy<TSpace> sizeStrategy,
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
      IOffsetStrategy<TSpace> offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(offsetStrategy, AreaStrategies<TSpace>.MaxArea(), subregionBuilder1);
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      IAreaStrategy<TSpace> sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(OffsetStrategies<TSpace>.MinOffset(), sizeStrategy, subregionBuilder1);
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(OffsetStrategies<TSpace>.MinOffset(), AreaStrategies<TSpace>.MaxArea(), subregionBuilder1);
    public static RegionBuilder1<TSpace, T1> Builder<T1>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1)
      where T1 : IRegion<TSpace>
      => Builder(ExplicitOffset(leftOffset, topOffset), ExplicitSize<TSpace>(width, height), subregionBuilder1);

    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      IOffsetStrategy<TSpace> offsetStrategy,
      IAreaStrategy<TSpace> sizeStrategy,
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
      IOffsetStrategy<TSpace> offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(offsetStrategy, MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      IAreaStrategy<TSpace> sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(NoOffset(), sizeStrategy, subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(NoOffset(), MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Horizontal<T1, T2>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Horizontal(ExplicitOffset(leftOffset, topOffset), ExplicitSize<TSpace>(width, height), subregionBuilder1, subregionBuilder2);

    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      IOffsetStrategy<TSpace> offsetStrategy,
      IAreaStrategy<TSpace> sizeStrategy,
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
      IOffsetStrategy<TSpace> offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(offsetStrategy, MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      IAreaStrategy<TSpace> sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(NoOffset(), sizeStrategy, subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(NoOffset(), MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<TSpace, T1, T2> Vertical<T1, T2>(
      uint leftOffset,
      uint topOffset,
      uint width,
      uint height,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      => Vertical(ExplicitOffset(leftOffset, topOffset), ExplicitSize<TSpace>(width, height), subregionBuilder1, subregionBuilder2);

    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      IOffsetStrategy<TSpace> offsetStrategy,
      IAreaStrategy<TSpace> sizeStrategy,
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
      IOffsetStrategy<TSpace> offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(offsetStrategy, MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      IAreaStrategy<TSpace> sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(NoOffset(), sizeStrategy, subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Horizontal<T1, T2, T3>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Horizontal(NoOffset(), MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
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
      => Horizontal(ExplicitOffset(leftOffset, topOffset), ExplicitSize<TSpace>(width, height), subregionBuilder1, subregionBuilder2, subregionBuilder3);

    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      IOffsetStrategy<TSpace> offsetStrategy,
      IAreaStrategy<TSpace> sizeStrategy,
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
      IOffsetStrategy<TSpace> offsetStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(offsetStrategy, MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      IAreaStrategy<TSpace> sizeStrategy,
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(NoOffset(), sizeStrategy, subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<TSpace, T1, T2, T3> Vertical<T1, T2, T3>(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
      where T1 : IRegion<TSpace>
      where T2 : IRegion<TSpace>
      where T3 : IRegion<TSpace>
      => Vertical(NoOffset(), MaxSize<TSpace>(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
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
      => Vertical(ExplicitOffset(leftOffset, topOffset), ExplicitSize<TSpace>(width, height), subregionBuilder1, subregionBuilder2, subregionBuilder3);
  }
}

