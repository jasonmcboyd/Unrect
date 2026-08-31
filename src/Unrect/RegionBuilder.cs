using Unrect.Core;

namespace Unrect
{
  public class RegionBuilder : RegionBuilderBase<Region>
  {
    public override Region Build(ISpace space) => new Region(space);
  }

  public class RegionBuilder1<T1> : RegionBuilderBase<Region1<T1>>
    where T1 : IRegion
  {
    public RegionBuilder1(IRegionBuilder<T1> subregionBuilder)
    {
      SubregionBuilder = subregionBuilder;
    }

    private IRegionBuilder<T1> SubregionBuilder { get; }

    public override Region1<T1> Build(ISpace space)
    {
      if (!SubspaceResolver.TryResolveSubspace(SubregionBuilder, space, out var subspace, out _, out _))
        throw new OutOfBoundsException();

      return new Region1<T1>(space, SubregionBuilder.Build(subspace));
    }
  }
}
