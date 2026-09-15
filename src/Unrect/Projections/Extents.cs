using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// How the engine cuts the region it is working in. Every extent it hands a projection is a
  /// <see cref="Plane{TSpace}"/>, and every one of them is made here.
  /// <para>
  /// <b>The rule: a plane the engine cuts has origin (0, 0), over a real subspace object.</b>
  /// A plane can translate arithmetically — that is what an origin is for — but the engine does not,
  /// because the streaming window's locus rides on the subspace object's own extent: a
  /// windowed space hands its offset and its height to the store with every cell it reads, and that
  /// pair is what tells a sweep of a bounded band apart from a walk down the sheet. Cut a region by
  /// arithmetic instead of by <see cref="ICellValues.GetSubspace"/> and the store stops being told
  /// which band is open — which moves the eviction counters and not the answers, the worst kind of
  /// change. So the three cutting methods refuse a translated plane outright rather than trusting
  /// the rule to be remembered; arithmetic cutting becomes safe only once the store is told
  /// directly instead of through the extent a slice carries.
  /// </para>
  /// <para>
  /// A bound is the exception that proves the rule: a region whose bottom edge is still being
  /// discovered carries the discovery on the plane, because no subspace object can represent a
  /// height nobody knows yet. The space underneath is still cut for real, so the locus is still told.
  /// </para>
  /// </summary>
  internal static class Extents
  {
    /// <summary>The whole of <paramref name="space"/>, as the region a projection is handed.</summary>
    internal static Plane<ICellValues> Extent(this ICellValues space) => Plane<ICellValues>.Of(space);

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>. Reads go through the region
    /// rather than through its space, so what a projection may read is what it was given — including
    /// a bottom edge still being discovered.
    /// </summary>
    internal static CellValue CellAt(this Plane<ICellValues> extent, int column, int row)
    {
      var point = extent[column, row];

      return point.Space[point.Column, point.Row];
    }

    /// <summary>
    /// The rectangle <paramref name="offset"/> into <paramref name="extent"/> and
    /// <paramref name="area"/> big. A named rectangle is a measured one, so what comes back carries
    /// no discovered bottom edge: asking for part of a region is not a question about the whole of
    /// it.
    /// </summary>
    internal static Plane<ICellValues> Cut(this Plane<ICellValues> extent, Offset offset, Area area)
    {
      Untranslated(extent);

      // The region's own check, which admits the rows asked for one at a time — where asking the
      // space would settle a bound merely to refuse.
      _ = extent.Slice(offset, area);

      return extent.Space.GetSubspace(offset, area).Extent();
    }

    /// <inheritdoc cref="Cut(Plane{ICellValues}, Offset, Area)"/>
    internal static Plane<ICellValues> Cut(this Plane<ICellValues> extent, Area area)
      => extent.Cut(default, area);

    /// <summary>
    /// Everything from <paramref name="offset"/> to the far edge, keeping an unsettled bottom edge
    /// unsettled — what a flow hands its next child and a repeat its next occurrence.
    /// </summary>
    internal static Plane<ICellValues> Tail(this Plane<ICellValues> extent, Offset offset)
    {
      Untranslated(extent);

      var rest = extent.Slice(offset);

      // Cut to the space's own remaining height rather than to the bound's: the bound hides the rows
      // below the boundary, and the space underneath stays measured so the scan still has somewhere
      // to look when it goes hunting for that boundary.
      var space = extent.Space.GetSubspace(
        offset,
        new Area(rest.Width, extent.Space.Area.Height - offset.Height));

      return rest.Bound is IBound bound ? space.Extent().Bounded(bound, rest.Width) : space.Extent();
    }

    /// <summary>
    /// The leading <paramref name="width"/> columns, keeping an unsettled bottom edge unsettled —
    /// the horizontal twin of <see cref="Tail"/>.
    /// </summary>
    internal static Plane<ICellValues> Narrow(this Plane<ICellValues> extent, int width)
    {
      Untranslated(extent);

      // A width past the edge is a bounds condition; a negative one is an argument bug, which is
      // what Area says about it two lines later.
      if (width > extent.Width)
        throw new OutOfBoundsException();

      var space = extent.Space.GetSubspace(new Offset(0, 0), new Area(width, extent.Space.Area.Height));

      return extent.Bound is IBound bound ? space.Extent().Bounded(bound, width) : space.Extent();
    }

    /// <summary>
    /// A space a strategy can be handed. A measured region is its own space; a region still being
    /// discovered puts one on, because the strategy layer still speaks spaces.
    /// </summary>
    internal static ICellValues AsSpace(this Plane<ICellValues> extent)
      => extent.Bound is null ? extent.Space : new BoundedView(extent);

    /// <summary>
    /// The space a region's cells actually come from — how a backend reaches past the region to ask
    /// its space something the region does not answer, such as a capability. One named place, so
    /// that when a region stops standing on a subspace object there is one line to change rather
    /// than a search for <c>.Space.Space</c>.
    /// </summary>
    internal static ICellValues Underlying(this Plane<ICellValues> extent) => extent.Space;

    /// <summary>
    /// The rule above, enforced rather than remembered. A translated region cut here would read the
    /// right cells and tell the streaming store the wrong band — a silent misread — so it is refused
    /// at the one door that could let it in.
    /// <para>
    /// It is a bug in this assembly and nothing a declaration can provoke, so it is an
    /// <see cref="EngineInvariantException"/> and therefore a fault: a guard a tolerance boundary
    /// could swallow would turn the misread it exists to prevent into a quietly empty result.
    /// </para>
    /// </summary>
    private static void Untranslated(Plane<ICellValues> extent)
    {
      if (extent.Origin.Width != 0 || extent.Origin.Height != 0)
        throw new EngineInvariantException(
          "The engine cuts real subspaces, so the region it cuts must start at its space's own corner; "
          + "a translated region cannot be cut here.");
    }
  }
}
