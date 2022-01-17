using System.Collections.Generic;
using System.Collections.Immutable;
using Unrect.Core;

namespace Unrect
{
  public abstract class RegionBase<TSpace> : IRegion<TSpace>
  {
    public RegionBase(ISpace<TSpace> space)
    {
      Space = space;
    }

    public ISpace<TSpace> Space { get; }
    public abstract IEnumerable<IRegion<TSpace>> GetSubregions();
  }

  public class Region<TSpace> : RegionBase<TSpace>
  {
    public Region(ISpace<TSpace> space) : base(space)
    {
    }

    public override IEnumerable<IRegion<TSpace>> GetSubregions()
    {
      yield break;
    }
  }

  public class Region1<TSpace, T1> : RegionBase<TSpace>
    where T1 : IRegion<TSpace>
  {
    public Region1(ISpace<TSpace> space, T1 subregion1) : base(space)
    {
      Subregion1 = subregion1;
    }

    public T1 Subregion1 { get; }

    public override IEnumerable<IRegion<TSpace>> GetSubregions()
    {
      yield return Subregion1;
    }
  }

  public class Region2<TSpace, T1, T2> : RegionBase<TSpace>
    where T1 : IRegion<TSpace>
    where T2 : IRegion<TSpace>
  {
    public Region2(
      ISpace<TSpace> space,
      T1 subregion1,
      T2 subregion2)
      : base(space)
    {
      Subregion1 = subregion1;
      Subregion2 = subregion2;
    }

    public T1 Subregion1 { get; }
    public T2 Subregion2 { get; }

    public override IEnumerable<IRegion<TSpace>> GetSubregions()
    {
      yield return Subregion1;
      yield return Subregion2;
    }
  }

  public class Region3<TSpace, T1, T2, T3> : RegionBase<TSpace>
    where T1 : IRegion<TSpace>
    where T2 : IRegion<TSpace>
    where T3 : IRegion<TSpace>
  {
    public Region3(
      ISpace<TSpace> space,
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

    public override IEnumerable<IRegion<TSpace>> GetSubregions()
    {
      yield return Subregion1;
      yield return Subregion2;
      yield return Subregion3;
    }
  }

  public class SuperRegion<TSpace, TSubregion> : RegionBase<TSpace>
    where TSubregion : IRegion<TSpace>
  {
    public SuperRegion(ISpace<TSpace> space, IEnumerable<TSubregion> subregions) : base(space)
    {
      Subregions = subregions.ToImmutableArray();
    }

    public ImmutableArray<TSubregion> Subregions { get; }

    public override IEnumerable<IRegion<TSpace>> GetSubregions()
    {
      foreach (IRegion<TSpace> region in Subregions)
        yield return region;
    }
  }
}
