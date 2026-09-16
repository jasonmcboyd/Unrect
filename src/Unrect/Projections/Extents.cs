using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// How the engine cuts the region it is working in. Every extent it hands a projection is a
  /// <see cref="Plane{TSpace}"/>, and every one of them is made here.
  /// <para>
  /// Cutting is arithmetic. A region is a space, an origin and an extent, so a subregion is the same
  /// space with a composed origin: nothing is allocated, nothing wraps anything, and a point minted
  /// through a cut names the very same cell as one minted through the region it was cut from.
  /// Decomposing a sheet into a hundred regions costs a hundred struct copies and no reads.
  /// </para>
  /// <para>
  /// A space that wants to know which band is open is told, once per placement, through
  /// <see cref="ISweepAware"/> — not by having its extent inferred from the shape of the objects
  /// handed around, which is what a cut used to say implicitly.
  /// </para>
  /// </summary>
  internal static class Extents
  {
    /// <summary>The whole of <paramref name="space"/>, as the region a projection is handed.</summary>
    internal static Plane<TSpace> Extent<TSpace>(this TSpace space)
      where TSpace : class, ISpace
      => Plane<TSpace>.Of(space);

    /// <summary>
    /// The rectangle <paramref name="offset"/> into <paramref name="extent"/> and
    /// <paramref name="area"/> big. A named rectangle is a measured one, so what comes back carries
    /// no discovered bottom edge: asking for part of a region is not a question about the whole of
    /// it.
    /// </summary>
    internal static Plane<TSpace> Cut<TSpace>(this Plane<TSpace> extent, Offset offset, Area area)
      where TSpace : class, ISpace
      => extent.Slice(offset, area);

    /// <inheritdoc cref="Cut{TSpace}(Plane{TSpace}, Offset, Area)"/>
    internal static Plane<TSpace> Cut<TSpace>(this Plane<TSpace> extent, Area area)
      where TSpace : class, ISpace
      => extent.Slice(area);

    /// <summary>
    /// Everything from <paramref name="offset"/> to the far edge, keeping an unsettled bottom edge
    /// unsettled — what a flow hands its next child and a repeat its next occurrence.
    /// </summary>
    internal static Plane<TSpace> Tail<TSpace>(this Plane<TSpace> extent, Offset offset)
      where TSpace : class, ISpace
      => extent.Slice(offset);

    /// <summary>
    /// The leading <paramref name="width"/> columns, keeping an unsettled bottom edge unsettled —
    /// the horizontal twin of <see cref="Tail{TSpace}"/>.
    /// </summary>
    internal static Plane<TSpace> Narrow<TSpace>(this Plane<TSpace> extent, int width)
      where TSpace : class, ISpace
      => extent.Narrowed(width);

    /// <summary>
    /// The region as the strategy calculus sees it: the canonical four and nothing else. One struct
    /// copy per strategy call — never per cell — and the discovered bottom edge rides along, so a
    /// strategy handed a region still being discovered forces it by asking its extent, exactly where
    /// it always did.
    /// </summary>
    internal static Plane<ISpace> AsCanonical<TSpace>(this Plane<TSpace> extent)
      where TSpace : class, ISpace
      => extent.Erased();
  }
}
