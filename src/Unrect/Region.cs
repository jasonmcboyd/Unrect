using System.Collections.Generic;
using System.Collections.Immutable;
using Unrect.Core;

namespace Unrect
{
  public abstract class RegionBase : IRegion
  {
    public RegionBase(ISpace space)
    {
      Space = space;
    }

    public ISpace Space { get; }
    public abstract IEnumerable<IRegion> GetSubregions();
  }

  public class Region : RegionBase
  {
    public Region(ISpace space) : base(space)
    {
    }

    public override IEnumerable<IRegion> GetSubregions()
    {
      yield break;
    }
  }

  public class Region1<T1> : RegionBase
    where T1 : IRegion
  {
    public Region1(ISpace space, T1 subregion1) : base(space)
    {
      Subregion1 = subregion1;
    }

    public T1 Subregion1 { get; }

    public override IEnumerable<IRegion> GetSubregions()
    {
      yield return Subregion1;
    }
  }

  public class Region2<T1, T2> : RegionBase
    where T1 : IRegion
    where T2 : IRegion
  {
    public Region2(
      ISpace space,
      T1 subregion1,
      T2 subregion2)
      : base(space)
    {
      Subregion1 = subregion1;
      Subregion2 = subregion2;
    }

    public T1 Subregion1 { get; }
    public T2 Subregion2 { get; }

    public override IEnumerable<IRegion> GetSubregions()
    {
      yield return Subregion1;
      yield return Subregion2;
    }
  }

  public class Region3<T1, T2, T3> : RegionBase
    where T1 : IRegion
    where T2 : IRegion
    where T3 : IRegion
  {
    public Region3(
      ISpace space,
      T1 subregion1,
      T2 subregion2,
      T3 subregion3)
      : base(space)
    {
      Subregion1 = subregion1;
      Subregion2 = subregion2;
      Subregion3 = subregion3;
    }

    public T1 Subregion1 { get; }
    public T2 Subregion2 { get; }
    public T3 Subregion3 { get; }

    public override IEnumerable<IRegion> GetSubregions()
    {
      yield return Subregion1;
      yield return Subregion2;
      yield return Subregion3;
    }
  }

  public class SuperRegion<TSubregion> : RegionBase
    where TSubregion : IRegion
  {
    public SuperRegion(ISpace space, IEnumerable<TSubregion> subregions) : base(space)
    {
      Subregions = subregions.ToImmutableArray();
    }

    public ImmutableArray<TSubregion> Subregions { get; }

    public override IEnumerable<IRegion> GetSubregions()
    {
      foreach (IRegion region in Subregions)
        yield return region;
    }
  }
}
