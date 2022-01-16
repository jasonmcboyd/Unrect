using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public class StackRegionBuilder2<TSpace, T1, T2> : StackRegionBuilderBase<TSpace, Region2<TSpace, T1, T2>>
    where T1 : IRegion<TSpace>
    where T2 : IRegion<TSpace>
  {
    public StackRegionBuilder2(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2)
    {
      SubregionBuilder1 = subregionBuilder1;
      SubregionBuilder2 = subregionBuilder2;
    }

    private IRegionBuilder<TSpace, T1> SubregionBuilder1 { get; }
    private IRegionBuilder<TSpace, T2> SubregionBuilder2 { get; }

    public override IEnumerable<IRegionBuilder> GetSubregionBuilders()
    {
      yield return SubregionBuilder1;
      yield return SubregionBuilder2;
    }

    public override Region2<TSpace, T1, T2> Build(ISpace<TSpace> space)
    {
      var subspaces = GetSubregionSpaces(space);

      return new Region2<TSpace, T1, T2>(
        space,
        SubregionBuilder1.Build(subspaces[0]),
        SubregionBuilder2.Build(subspaces[1]));
    }
  }

  public class StackRegionBuilder3<TSpace, T1, T2, T3> : StackRegionBuilderBase<TSpace, Region3<TSpace, T1, T2, T3>>
    where T1 : IRegion<TSpace>
    where T2 : IRegion<TSpace>
    where T3 : IRegion<TSpace>
  {
    public StackRegionBuilder3(
      IRegionBuilder<TSpace, T1> subregionBuilder1,
      IRegionBuilder<TSpace, T2> subregionBuilder2,
      IRegionBuilder<TSpace, T3> subregionBuilder3)
    {
      SubregionBuilder1 = subregionBuilder1;
      SubregionBuilder2 = subregionBuilder2;
      SubregionBuilder3 = subregionBuilder3;
    }

    private IRegionBuilder<TSpace, T1> SubregionBuilder1 { get; }
    private IRegionBuilder<TSpace, T2> SubregionBuilder2 { get; }
    private IRegionBuilder<TSpace, T3> SubregionBuilder3 { get; }

    public override IEnumerable<IRegionBuilder> GetSubregionBuilders()
    {
      yield return SubregionBuilder1;
      yield return SubregionBuilder2;
      yield return SubregionBuilder3;
    }

    public override Region3<TSpace, T1, T2, T3> Build(ISpace<TSpace> space)
    {
      var subspaces = GetSubregionSpaces(space);

      return new Region3<TSpace, T1, T2, T3>(
        space,
        SubregionBuilder1.Build(subspaces[0]),
        SubregionBuilder2.Build(subspaces[1]),
        SubregionBuilder3.Build(subspaces[2]));
    }
  }
}
