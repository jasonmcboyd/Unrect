using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public class SuperStackRegionBuilder<TRegion> : RegionBuilderBase<SuperRegion<TRegion>>
    where TRegion : IRegion
  {
    public SuperStackRegionBuilder(IRegionBuilder<TRegion> subregionBuilder)
    {
      SubregionBuilder = subregionBuilder;
    }

    public IRegionBuilder<TRegion> SubregionBuilder { get; }
    public Orientation Orientation { get; init; } = Orientation.Vertical;

    public override SuperRegion<TRegion> Build(ISpace space)
    {
      var subregions = new List<TRegion>();
      var remainingSpace = space;

      while (remainingSpace.Area.Size.Width > 0 && remainingSpace.Area.Size.Height > 0)
      {
        if (!SubspaceResolver.TryResolveSubspace(SubregionBuilder, remainingSpace, out var subspace, out var offset, out var area))
          break;

        if (area.Size.Width == 0 || area.Size.Height == 0)
          break;

        subregions.Add(SubregionBuilder.Build(subspace));

        var advance =
          Orientation == Orientation.Horizontal
          ? new Offset(offset.Size.Width + area.Size.Width, 0)
          : new Offset(0, offset.Size.Height + area.Size.Height);

        if (advance.Size.Width == 0 && advance.Size.Height == 0)
          break;

        remainingSpace = remainingSpace.GetSubspace(advance);
      }

      return new SuperRegion<TRegion>(space, subregions);
    }
  }
}
