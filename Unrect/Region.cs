using System.Collections.Generic;
using System.Collections.Immutable;
using Unrect.Core;

namespace Unrect
{
  public class Region<TSpace> : IRegion<TSpace>
  {
    public Region(ISpace<TSpace> space)
    {
      Space = space;
    }

    public ISpace<TSpace> Space { get; }
  }

  public class Region1<TSpace, T1> : Region<TSpace>
    where T1 : IRegion<TSpace>
  {
    public Region1(ISpace<TSpace> space, T1 subregion1) : base(space)
    {
      Subregion1 = subregion1;
    }

    public T1 Subregion1 { get; }
  }

  public class Region2<TSpace, T1, T2> : Region<TSpace>
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
  }

  public class Region3<TSpace, T1, T2, T3> : Region<TSpace>
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
  }

  public class SuperRegion<TSpace, TSubregion> : Region<TSpace>
    where TSubregion : IRegion<TSpace>
  {
    public SuperRegion(ISpace<TSpace> space, IEnumerable<TSubregion> subregions) : base(space)
    {
      Subregions = subregions.ToImmutableArray();
    }

    public ImmutableArray<TSubregion> Subregions { get; }
  }
}
