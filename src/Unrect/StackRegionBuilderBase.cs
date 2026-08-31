using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public abstract class StackRegionBuilderBase<T1> : RegionBuilderBase<T1>
    where T1 : IRegion
  {
    public Orientation Orientation { get; init; } = Orientation.Vertical;

    protected abstract IEnumerable<IRegionBuilder> GetSubregionBuilders();

    protected List<ISpace> GetSubregionSpaces(ISpace space)
    {
      var result = new List<ISpace>();

      foreach (var subregionBuilder in GetSubregionBuilders())
      {
        if (!SubspaceResolver.TryResolveSubspace(subregionBuilder, space, out var subspace, out var offset, out var area))
          throw new OutOfBoundsException();

        result.Add(subspace);

        space =
          Orientation == Orientation.Horizontal
          ? space.GetSubspace(new Offset(offset.Size.Width + area.Size.Width, 0))
          : space.GetSubspace(new Offset(0, offset.Size.Height + area.Size.Height));
      }

      return result;
    }
  }
}
