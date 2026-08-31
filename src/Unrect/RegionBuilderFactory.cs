using Unrect.Core;
using static Unrect.Strategies.AreaStrategies;
using static Unrect.Strategies.OffsetStrategies;

namespace Unrect
{
  public static class RegionBuilderFactory
  {
    public static RegionBuilder Builder(
      IOffsetStrategy offsetStrategy,
      IAreaStrategy areaStrategy)
    {
      return new RegionBuilder()
      {
        OffsetStrategy = offsetStrategy,
        AreaStrategy = areaStrategy,
      };
    }
    public static RegionBuilder Builder(IOffsetStrategy offsetStrategy) => Builder(offsetStrategy, MaxArea());
    public static RegionBuilder Builder(IAreaStrategy areaStrategy) => Builder(MinOffset(), areaStrategy);
    public static RegionBuilder Builder() => Builder(MinOffset(), MaxArea());
    public static RegionBuilder Builder(int leftOffset, int topOffset, int width, int height)
      => Builder(ExplicitOffset(leftOffset, topOffset), ExplicitArea(width, height));
    public static RegionBuilder Builder(int leftOffset, int topOffset, IAreaStrategy areaStrategy)
      => Builder(ExplicitOffset(leftOffset, topOffset), areaStrategy);
    public static RegionBuilder Builder(int width, int height, IOffsetStrategy offsetStrategy)
      => Builder(offsetStrategy, ExplicitArea(width, height));

    public static RegionBuilder1<T1> Builder<T1>(
      IOffsetStrategy offsetStrategy,
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1)
      where T1 : IRegion
    {
      return new RegionBuilder1<T1>(subregionBuilder1)
      {
        OffsetStrategy = offsetStrategy,
        AreaStrategy = areaStrategy
      };
    }
    public static RegionBuilder1<T1> Builder<T1>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<T1> subregionBuilder1)
      where T1 : IRegion
      => Builder(offsetStrategy, MaxArea(), subregionBuilder1);
    public static RegionBuilder1<T1> Builder<T1>(
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1)
      where T1 : IRegion
      => Builder(MinOffset(), areaStrategy, subregionBuilder1);
    public static RegionBuilder1<T1> Builder<T1>(
      IRegionBuilder<T1> subregionBuilder1)
      where T1 : IRegion
      => Builder(MinOffset(), MaxArea(), subregionBuilder1);
    public static RegionBuilder1<T1> Builder<T1>(
      int leftOffset,
      int topOffset,
      int width,
      int height,
      IRegionBuilder<T1> subregionBuilder1)
      where T1 : IRegion
      => Builder(ExplicitOffset(leftOffset, topOffset), ExplicitArea(width, height), subregionBuilder1);

    public static SuperStackRegionBuilder<TRegion> Repeat<TRegion>(
      IRegionBuilder<TRegion> subregionBuilder)
      where TRegion : IRegion
      => new SuperStackRegionBuilder<TRegion>(subregionBuilder);

    public static SuperStackRegionBuilder<TRegion> RepeatHorizontal<TRegion>(
      IRegionBuilder<TRegion> subregionBuilder)
      where TRegion : IRegion
      => new SuperStackRegionBuilder<TRegion>(subregionBuilder) { Orientation = Orientation.Horizontal };

    public static StackRegionBuilder2<T1, T2> Horizontal<T1, T2>(
      IOffsetStrategy offsetStrategy,
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
    {
      return new StackRegionBuilder2<T1, T2>(subregionBuilder1, subregionBuilder2)
      {
        OffsetStrategy = offsetStrategy,
        AreaStrategy = areaStrategy,
        Orientation = Orientation.Horizontal
      };
    }
    public static StackRegionBuilder2<T1, T2> Horizontal<T1, T2>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Horizontal(offsetStrategy, MaxArea(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<T1, T2> Horizontal<T1, T2>(
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Horizontal(MinOffset(), areaStrategy, subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<T1, T2> Horizontal<T1, T2>(
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Horizontal(MinOffset(), MaxArea(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<T1, T2> Horizontal<T1, T2>(
      int leftOffset,
      int topOffset,
      int width,
      int height,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Horizontal(ExplicitOffset(leftOffset, topOffset), ExplicitArea(width, height), subregionBuilder1, subregionBuilder2);

    public static StackRegionBuilder2<T1, T2> Vertical<T1, T2>(
      IOffsetStrategy offsetStrategy,
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
    {
      return new StackRegionBuilder2<T1, T2>(subregionBuilder1, subregionBuilder2)
      {
        OffsetStrategy = offsetStrategy,
        AreaStrategy = areaStrategy
      };
    }
    public static StackRegionBuilder2<T1, T2> Vertical<T1, T2>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Vertical(offsetStrategy, MaxArea(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<T1, T2> Vertical<T1, T2>(
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Vertical(MinOffset(), areaStrategy, subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<T1, T2> Vertical<T1, T2>(
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Vertical(MinOffset(), MaxArea(), subregionBuilder1, subregionBuilder2);
    public static StackRegionBuilder2<T1, T2> Vertical<T1, T2>(
      int leftOffset,
      int topOffset,
      int width,
      int height,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
      where T1 : IRegion
      where T2 : IRegion
      => Vertical(ExplicitOffset(leftOffset, topOffset), ExplicitArea(width, height), subregionBuilder1, subregionBuilder2);

    public static StackRegionBuilder3<T1, T2, T3> Horizontal<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
    {
      return new StackRegionBuilder3<T1, T2, T3>(subregionBuilder1, subregionBuilder2, subregionBuilder3)
      {
        OffsetStrategy = offsetStrategy,
        AreaStrategy = areaStrategy,
        Orientation = Orientation.Horizontal
      };
    }
    public static StackRegionBuilder3<T1, T2, T3> Horizontal<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Horizontal(offsetStrategy, MaxArea(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<T1, T2, T3> Horizontal<T1, T2, T3>(
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Horizontal(MinOffset(), areaStrategy, subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<T1, T2, T3> Horizontal<T1, T2, T3>(
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Horizontal(MinOffset(), MaxArea(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<T1, T2, T3> Horizontal<T1, T2, T3>(
      int leftOffset,
      int topOffset,
      int width,
      int height,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Horizontal(ExplicitOffset(leftOffset, topOffset), ExplicitArea(width, height), subregionBuilder1, subregionBuilder2, subregionBuilder3);

    public static StackRegionBuilder3<T1, T2, T3> Vertical<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
    {
      return new StackRegionBuilder3<T1, T2, T3>(subregionBuilder1, subregionBuilder2, subregionBuilder3)
      {
        OffsetStrategy = offsetStrategy,
        AreaStrategy = areaStrategy
      };
    }
    public static StackRegionBuilder3<T1, T2, T3> Vertical<T1, T2, T3>(
      IOffsetStrategy offsetStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Vertical(offsetStrategy, MaxArea(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<T1, T2, T3> Vertical<T1, T2, T3>(
      IAreaStrategy areaStrategy,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Vertical(MinOffset(), areaStrategy, subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<T1, T2, T3> Vertical<T1, T2, T3>(
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Vertical(MinOffset(), MaxArea(), subregionBuilder1, subregionBuilder2, subregionBuilder3);
    public static StackRegionBuilder3<T1, T2, T3> Vertical<T1, T2, T3>(
      int leftOffset,
      int topOffset,
      int width,
      int height,
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
      where T1 : IRegion
      where T2 : IRegion
      where T3 : IRegion
      => Vertical(ExplicitOffset(leftOffset, topOffset), ExplicitArea(width, height), subregionBuilder1, subregionBuilder2, subregionBuilder3);
  }
}
