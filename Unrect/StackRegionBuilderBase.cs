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
      ? (offset.Width, offset.Height)
      : (offset.Height, offset.Width);

    private (uint OrientedLength, uint UnorientedLength) GetOrientedSize(Core.Offset size) =>
      Orientation == Orientation.Horizontal
      ? (size.Width, size.Height)
      : (size.Height, size.Width);

    protected List<ISpace<TSpace>> GetSubregionSpaces(ISpace<TSpace> space)
    {
      var result = new List<ISpace<TSpace>>();

      foreach (var subregionBuilder in GetSubregionBuilders())
      {
        var subregionOffset = subregionBuilder.OffsetStrategy.GetSize();
        if (subregionOffset.Width > space.Area.Width || subregionOffset.Height > space.Area.Height)
        {
          throw new OutOfBoundsException();
        }

        var availableSpace = space.GetSubspace(subregionOffset);

        var subregionSize = subregionBuilder.SizeStrategy.GetSize(availableSpace);
        if (subregionSize.Width > availableSpace.Area.Width || subregionSize.Height > availableSpace.Area.Height)
        {
          throw new OutOfBoundsException();
        }

        result.Add(availableSpace.GetSubspace(subregionSize));
        
        space =
          Orientation == Orientation.Horizontal
          ? space.GetSubspace(new Core.Offset(subregionOffset.Width + subregionSize.Width, 0))
          : space.GetSubspace(new Core.Offset(0, subregionOffset.Height + subregionSize.Height));
      }

      return result;
    }
  }
}
