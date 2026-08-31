using Unrect.Core;

namespace Unrect
{
  internal static class SubspaceResolver
  {
    /// <summary>
    /// Applies a builder's offset and area strategies to <paramref name="space"/>, yielding the
    /// subspace the builder describes along with the offset and area that produced it. Returns
    /// false when either strategy asks for more space than is available; callers decide whether
    /// that is an error or a stopping condition.
    /// </summary>
    public static bool TryResolveSubspace(
      IRegionBuilder builder,
      ISpace space,
      out ISpace subspace,
      out Offset offset,
      out Area area)
    {
      subspace = space;
      area = default;

      offset = builder.OffsetStrategy.GetOffset(space);
      if (offset.Size.Width > space.Area.Size.Width || offset.Size.Height > space.Area.Size.Height)
        return false;

      var availableSpace = space.GetSubspace(offset);

      area = builder.AreaStrategy.GetArea(availableSpace);
      if (area.Size.Width > availableSpace.Area.Size.Width || area.Size.Height > availableSpace.Area.Size.Height)
        return false;

      subspace = availableSpace.GetSubspace(area);
      return true;
    }
  }
}
