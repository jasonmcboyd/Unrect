using System;
using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public class SuperStackRegionBuilder<TSpace, TRegion> : StackRegionBuilderBase<TSpace, SuperRegion<TSpace, TRegion>>
    where TRegion : IRegion<TSpace>
  {
    public SuperStackRegionBuilder(
      Func<IRegionBuilder<TSpace, TRegion>> subregionBuilderFactory)
    {
      SubregionBuilderFactory = subregionBuilderFactory;
    }

    public Func<IRegionBuilder<TSpace, TRegion>> SubregionBuilderFactory { get; }

    protected override IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders()
    {
      while (true)
        yield return SubregionBuilderFactory();
    }

    public override SuperRegion<TSpace, TRegion> Build(ISpace<TSpace> space)
    {
      var subregions = new List<TRegion>();
      var remainingSpace = space;

      while (remainingSpace.Area.Size.Width > 0 && remainingSpace.Area.Size.Height > 0)
      {
        var builder = SubregionBuilderFactory();

        var subregionOffset = builder.OffsetStrategy.GetOffset(remainingSpace);
        if (subregionOffset.Size.Width > remainingSpace.Area.Size.Width || subregionOffset.Size.Height > remainingSpace.Area.Size.Height)
          break;

        var availableSpace = remainingSpace.GetSubspace(subregionOffset);

        var subregionArea = builder.AreaStrategy.GetArea(availableSpace);
        if (subregionArea.Size.Width > availableSpace.Area.Size.Width || subregionArea.Size.Height > availableSpace.Area.Size.Height)
          break;

        var subspace = availableSpace.GetSubspace(subregionArea);
        subregions.Add(builder.Build(subspace));

        remainingSpace =
          Orientation == Orientation.Horizontal
          ? remainingSpace.GetSubspace(new Offset(subregionOffset.Size.Width + subregionArea.Size.Width, 0))
          : remainingSpace.GetSubspace(new Offset(0, subregionOffset.Size.Height + subregionArea.Size.Height));
      }

      return new SuperRegion<TSpace, TRegion>(space, subregions);
    }
  }
}
