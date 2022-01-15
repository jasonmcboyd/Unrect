using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public abstract class StackRegionBuilderBase<TSpace, T1> : RegionBuilderBase<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public Orientation Orientation { get; } = Orientation.Vertical;
    protected (uint OrientedLength, uint UnorientedLength) GetOrientedOffset(Offset offset) =>
      Orientation == Orientation.Horizontal
      ? (offset.LeftOffset, offset.TopOffset)
      : (offset.TopOffset, offset.LeftOffset);

    protected (uint OrientedLength, uint UnorientedLength) GetOrientedSize(Size size) =>
      Orientation == Orientation.Horizontal
      ? (size.Width, size.Height)
      : (size.Height, size.Width);

    //public abstract IEnumerable<IRegionBuilder> GetSubregionBuilders();

    //protected IEnumerable
  }
}
