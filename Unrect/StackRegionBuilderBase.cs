using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public abstract class StackRegionBuilderBase<TSpace, T1> : RegionBuilderBase<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public Orientation Orientation { get; init; } = Orientation.Vertical;

    private (uint OrientedLength, uint UnorientedLength) GetOrientedOffset(Core.Offset offset) =>
      Orientation == Orientation.Horizontal
      ? (offset.Size.Width, offset.Size.Height)
      : (offset.Size.Height, offset.Size.Width);

    private (uint OrientedLength, uint UnorientedLength) GetOrientedSize(Core.Offset offset) =>
      Orientation == Orientation.Horizontal
      ? (offset.Size.Width, offset.Size.Height)
      : (offset.Size.Height, offset.Size.Width);

    protected List<ISpace<TSpace>> GetSubregionSpaces(ISpace<TSpace> space)
    {
      var result = new List<ISpace<TSpace>>();

      foreach (var subregionBuilder in GetSubregionBuilders())
      {
        var subregionOffset = subregionBuilder.OffsetStrategy.GetOffset(space);
        if (subregionOffset.Size.Width > space.Area.Size.Width || subregionOffset.Size.Height > space.Area.Size.Height)
        {
          throw new OutOfBoundsException();
        }

        var availableSpace = space.GetSubspace(subregionOffset);

        var subregionSize = subregionBuilder.AreaStrategy.GetArea(availableSpace);
        if (subregionSize.Size.Width > availableSpace.Area.Size.Width || subregionSize.Size.Height > availableSpace.Area.Size.Height)
        {
          throw new OutOfBoundsException();
        }

        result.Add(availableSpace.GetSubspace(subregionSize));
        
        space =
          Orientation == Orientation.Horizontal
          ? space.GetSubspace(new Core.Offset(subregionOffset.Size.Width + subregionSize.Size.Width, 0))
          : space.GetSubspace(new Core.Offset(0, subregionOffset.Size.Height + subregionSize.Size.Height));
      }

      return result;
    }
  }
}
