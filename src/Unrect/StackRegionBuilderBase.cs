using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public abstract class StackRegionBuilderBase<TSpace, T1> : RegionBuilderBase<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public Orientation Orientation { get; init; } = Orientation.Vertical;

    protected abstract IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders();

    protected List<ISpace<TSpace>> GetSubregionSpaces(ISpace<TSpace> space, bool throwWhenOutOfBounds)
    {
      var result = new List<ISpace<TSpace>>();

      foreach (var subregionBuilder in GetSubregionBuilders())
      {
        var subregionOffset = subregionBuilder.OffsetStrategy.GetOffset(space);
        if (subregionOffset.Size.Width > space.Area.Size.Width || subregionOffset.Size.Height > space.Area.Size.Height)
        {
          if (throwWhenOutOfBounds)
            throw new OutOfBoundsException();
          else
            break;
        }

        var availableSpace = space.GetSubspace(subregionOffset);

        var subregionSize = subregionBuilder.AreaStrategy.GetArea(availableSpace);
        if (subregionSize.Size.Width > availableSpace.Area.Size.Width || subregionSize.Size.Height > availableSpace.Area.Size.Height)
        {
          if (throwWhenOutOfBounds)
            throw new OutOfBoundsException();
          else
            break;
        }

        result.Add(availableSpace.GetSubspace(subregionSize));
        
        space =
          Orientation == Orientation.Horizontal
          ? space.GetSubspace(new Offset(subregionOffset.Size.Width + subregionSize.Size.Width, 0))
          : space.GetSubspace(new Offset(0, subregionOffset.Size.Height + subregionSize.Size.Height));
      }

      return result;
    }
  }
}
