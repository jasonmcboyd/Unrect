using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public class StackRegionBuilder2<T1, T2> : StackRegionBuilderBase<Region2<T1, T2>>
    where T1 : IRegion
    where T2 : IRegion
  {
    public StackRegionBuilder2(
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2)
    {
      SubregionBuilder1 = subregionBuilder1;
      SubregionBuilder2 = subregionBuilder2;
    }

    private IRegionBuilder<T1> SubregionBuilder1 { get; }
    private IRegionBuilder<T2> SubregionBuilder2 { get; }

    protected override IEnumerable<IRegionBuilder> GetSubregionBuilders()
    {
      yield return SubregionBuilder1;
      yield return SubregionBuilder2;
    }

    public override Region2<T1, T2> Build(ISpace space)
    {
      var subspaces = GetSubregionSpaces(space);

      return new Region2<T1, T2>(
        space,
        SubregionBuilder1.Build(subspaces[0]),
        SubregionBuilder2.Build(subspaces[1]));
    }
  }

  public class StackRegionBuilder3<T1, T2, T3> : StackRegionBuilderBase<Region3<T1, T2, T3>>
    where T1 : IRegion
    where T2 : IRegion
    where T3 : IRegion
  {
    public StackRegionBuilder3(
      IRegionBuilder<T1> subregionBuilder1,
      IRegionBuilder<T2> subregionBuilder2,
      IRegionBuilder<T3> subregionBuilder3)
    {
      SubregionBuilder1 = subregionBuilder1;
      SubregionBuilder2 = subregionBuilder2;
      SubregionBuilder3 = subregionBuilder3;
    }

    private IRegionBuilder<T1> SubregionBuilder1 { get; }
    private IRegionBuilder<T2> SubregionBuilder2 { get; }
    private IRegionBuilder<T3> SubregionBuilder3 { get; }

    protected override IEnumerable<IRegionBuilder> GetSubregionBuilders()
    {
      yield return SubregionBuilder1;
      yield return SubregionBuilder2;
      yield return SubregionBuilder3;
    }

    public override Region3<T1, T2, T3> Build(ISpace space)
    {
      var subspaces = GetSubregionSpaces(space);

      return new Region3<T1, T2, T3>(
        space,
        SubregionBuilder1.Build(subspaces[0]),
        SubregionBuilder2.Build(subspaces[1]),
        SubregionBuilder3.Build(subspaces[2]));
    }
  }
}
