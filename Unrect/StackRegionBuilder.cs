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

    private IRegionBuilder<TSpace, T1> SubregionBuilder1 { get; init; }
    private IRegionBuilder<TSpace, T2> SubregionBuilder2 { get; init; }

    public override Region2<TSpace, T1, T2> Build(ISpace<TSpace> space)
    {
      var subregionBuilders = new IRegionBuilder[] { SubregionBuilder1, SubregionBuilder2 };

      uint cumulativeOrientedOffsetLength = 0;

      var subspaces = new ISpace<TSpace>[2];

      for (int i = 0; i < subregionBuilders.Length; i++)
      {
        var subregionBuilder = subregionBuilders[i];

        var orientedOffset = GetOrientedOffset(subregionBuilder.OffsetStrategy.GetOffset());
        var orientedSize = GetOrientedSize(subregionBuilder.SizeStrategy.GetSize(space.Size));
        var spaceOrientedSize = GetOrientedSize(space.Size);

        if (cumulativeOrientedOffsetLength + orientedOffset.OrientedLength + orientedSize.OrientedLength > spaceOrientedSize.OrientedLength)
        {
          throw new OutOfBoundsException();
        }

        if (orientedOffset.UnorientedLength + orientedSize.UnorientedLength > spaceOrientedSize.UnorientedLength)
        {
          throw new OutOfBoundsException();
        }

        var cumulativeOrientedOffset =
          Orientation == Orientation.Horizontal
          ? new Offset(cumulativeOrientedOffsetLength, 0)
          : new Offset(0, cumulativeOrientedOffsetLength);

        subspaces[i] = space.GetSubspace(subregionBuilder.OffsetStrategy.GetOffset() + cumulativeOrientedOffset, subregionBuilder.SizeStrategy.GetSize(space.Size));

        cumulativeOrientedOffsetLength += orientedOffset.OrientedLength + orientedSize.OrientedLength;
      }

      return new Region2<TSpace, T1, T2>(space, SubregionBuilder1.Build(subspaces[0]), SubregionBuilder2.Build(subspaces[1]));
    }
  }
}
