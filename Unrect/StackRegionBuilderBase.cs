using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public abstract class StackRegionBuilderBase<TSpace, T1> : RegionBuilderBase<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public Orientation Orientation { get; init; } = Orientation.Vertical;

    private (uint OrientedLength, uint UnorientedLength) GetOrientedOffset(Offset offset) =>
      Orientation == Orientation.Horizontal
      ? (offset.LeftOffset, offset.TopOffset)
      : (offset.TopOffset, offset.LeftOffset);

    private (uint OrientedLength, uint UnorientedLength) GetOrientedSize(Size size) =>
      Orientation == Orientation.Horizontal
      ? (size.Width, size.Height)
      : (size.Height, size.Width);

    protected List<ISpace<TSpace>> GetSubregionSpaces(ISpace<TSpace> space)
    {
      var result = new List<ISpace<TSpace>>();

      foreach (var subregionBuilder in GetSubregionBuilders())
      {
        var subregionOffset = subregionBuilder.OffsetStrategy.GetOffset();
        if (subregionOffset.LeftOffset > space.Size.Width || subregionOffset.TopOffset > space.Size.Height)
        {
          throw new OutOfBoundsException();
        }

        var availableSpace = space.GetSubspace(subregionOffset);

        var subregionSize = subregionBuilder.SizeStrategy.GetSize(availableSpace.Size);
        if (subregionSize.Width > availableSpace.Size.Width || subregionSize.Height > availableSpace.Size.Height)
        {
          throw new OutOfBoundsException();
        }

        result.Add(availableSpace.GetSubspace(subregionSize));
        
        space =
          Orientation == Orientation.Horizontal
          ? space.GetSubspace(new Offset(subregionOffset.LeftOffset + subregionSize.Width, 0))
          : space.GetSubspace(new Offset(0, subregionOffset.TopOffset + subregionSize.Height));
      }

      return result;
    }
  }
}
