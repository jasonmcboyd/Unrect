﻿using System.Collections.Generic;
using Unrect.Core;

namespace Unrect
{
  public class RegionBuilder<TSpace> : RegionBuilderBase<TSpace, Region<TSpace>>
  {
    public override Region<TSpace> Build(ISpace<TSpace> space) => new Region<TSpace>(space);

    public override IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders()
    {
      yield break;
    }
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
      var size = SubregionBuilder.SizeStrategy.GetArea(space);

      if (offset.Width + size.Width > space.Area.Width)
      {
        throw new OutOfBoundsException();
      }

      if (offset.Height + size.Height > space.Area.Height)
      {
        throw new OutOfBoundsException();
      }

      var subspace = space.GetSubspace(offset, size);
      var subregion = SubregionBuilder.Build(subspace);
      return new Region1<TSpace, TRegion>(space, subregion);
    }

    public override IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders()
    {
      yield return SubregionBuilder;
    }
  }
}
