using Unrect.Core;

namespace Unrect
{
  public class RegionBuilder<TSpace> : RegionBuilderBase<TSpace, Region<TSpace>>
  {
    public override Region<TSpace> Build(ISpace<TSpace> space) => new Region<TSpace>(space);
  }

  public class RegionBuilder1<TSpace, TRegion> : RegionBuilderBase<TSpace, Region1<TSpace, TRegion>>
    where TRegion : IRegion<TSpace>
  {
    public RegionBuilder1(IRegionBuilder<TSpace, TRegion> subregionBuilder)
    {
      SubregionBuilder = subregionBuilder;
    }

    private IRegionBuilder<TSpace, TRegion> SubregionBuilder { get; }

    public override Region1<TSpace, TRegion> Build(ISpace<TSpace> space)
    {
      var offset = SubregionBuilder.OffsetStrategy.GetOffset();
      space = space.GetSubspace(offset);
      var size = SubregionBuilder.SizeStrategy.GetSize(space.Size);

      if (offset.LeftOffset + size.Width > space.Size.Width)
      {
        throw new OutOfBoundsException();
      }

      if (offset.TopOffset + size.Height > space.Size.Height)
      {
        throw new OutOfBoundsException();
      }

      var subspace = space.GetSubspace(offset, size);
      var subregion = SubregionBuilder.Build(subspace);
      return new Region1<TSpace, TRegion>(space, subregion);
    }
  }
}
