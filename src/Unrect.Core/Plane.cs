using System;
using System.Runtime.CompilerServices;

namespace Unrect.Core
{
  /// <summary>
  /// A rectangular region of one space: which space, where the region starts in that space's own
  /// coordinates, and how big it is. The 2-dimensional locator — what a declaration is handed, and
  /// what it hands its children.
  /// <para>
  /// <b>Slicing is arithmetic.</b> A plane holds a root origin rather than a wrapped space, so a
  /// subregion is the same space with a composed origin: nothing is allocated, nothing wraps
  /// anything, and a point minted through a slice names the very same cell as one minted through
  /// the parent at the translated coordinate. Decomposing a sheet into a hundred regions costs a
  /// hundred struct copies and no reads.
  /// </para>
  /// <para>
  /// <b>It is also where coordinates are checked.</b> A space refuses a cell outside its own edge;
  /// a plane refuses one outside the region it names, which is the narrower and more useful
  /// question — running off the end of the region a declaration was given is exactly how that
  /// declaration discovers it has run out of room. The refusal is
  /// <see cref="OutOfBoundsException"/>, at the slice or at the mint, never deferred to the read.
  /// </para>
  /// <para>
  /// Equality is value equality over the three parts, with the space compared by reference: two
  /// planes are equal when they name the same region of the same space.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space this plane names a region of.</typeparam>
  public readonly struct Plane<TSpace> : IEquatable<Plane<TSpace>>
    where TSpace : class, ISpace
  {
    /// <summary>The region's own extent — the declared one, whose height is a ceiling once <see cref="_bound"/> is set.</summary>
    private readonly Area _extent;

    /// <summary>The bottom edge still being discovered, or null when the region is measured.</summary>
    private readonly IBound? _bound;

    /// <summary>
    /// The region of <paramref name="space"/> starting at <paramref name="origin"/> and
    /// <paramref name="area"/> big, in that space's own coordinates.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> is null.</exception>
    /// <exception cref="OutOfBoundsException">The region does not fit inside the space.</exception>
    public Plane(TSpace space, Offset origin, Area area)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var extent = space.Area;

      // Compared by subtraction rather than by adding the two terms, because the sum can overflow:
      // every component here is non-negative (a Size refuses a negative), so an origin near
      // int.MaxValue plus any area at all wraps to a negative that passes a "> width" test, and the
      // region would then be built past the edge and fail later as an argument bug — a fault, which
      // no tolerance boundary may absorb — instead of as the bounds condition it is.
      if (origin.Width > extent.Width - area.Width || origin.Height > extent.Height - area.Height)
        throw new OutOfBoundsException();

      Space = space;
      Origin = origin;
      _extent = area;
      _bound = null;
    }

    /// <summary>
    /// A region already known to fit, with the bottom edge <paramref name="bound"/> decides. Private
    /// because every way in has just checked what it is about to build.
    /// </summary>
    private Plane(TSpace space, Offset origin, Area extent, IBound? bound)
    {
      Space = space;
      Origin = origin;
      _extent = extent;
      _bound = bound;
    }

    /// <summary>The whole of <paramref name="space"/> — the plane a declaration starts from.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> is null.</exception>
    public static Plane<TSpace> Of(TSpace space)
      => space is null
        ? throw new ArgumentNullException(nameof(space))
        : new Plane<TSpace>(space, default, space.Area, null);

    /// <summary>The space this plane names a region of, and reads through.</summary>
    public TSpace Space { get; }

    /// <summary>Where the region starts, in <see cref="Space"/>'s own coordinates.</summary>
    public Offset Origin { get; }

    /// <summary>
    /// How wide the region is. Free, and never settles a discovered bottom edge — which is the half
    /// of <see cref="Area"/> a forward-only reader can have for nothing.
    /// </summary>
    public int Width => _extent.Width;

    /// <summary>
    /// The rule deciding where the region ends, or null when its height is simply measured. Reading
    /// it settles nothing — it is the discovery itself, not its answer.
    /// </summary>
    public IBound? Bound => _bound;

    /// <summary>
    /// The extent as declared: what the region claims, which for a bottom edge still being
    /// discovered is the ceiling that edge sits under rather than where it turns out to be. Free,
    /// because it asks nothing — which is what makes it the honest answer where
    /// <see cref="Area"/> would read a file to give one.
    /// </summary>
    internal Area Declared => _extent;

    /// <summary>
    /// How big the region is — and <b>asking settles a discovered bottom edge</b>, reading the
    /// discovery to exhaustion. An extent is a pair of numbers and there is no answering half of
    /// one, so a reader that must not force asks <see cref="Width"/> and <see cref="HasRow"/>
    /// instead.
    /// </summary>
    public Area Area => _bound is null ? _extent : new Area(_extent.Width, _bound.Force());

    /// <summary>
    /// Whether the region has a row at <paramref name="row"/> — the question a forward-only reader
    /// asks instead of "how tall are you". A discovered bottom edge reads only as far as it takes to
    /// answer; a measured one compares against its height.
    /// <para>
    /// A discovered edge is bounded by the declared height too. The discovery cannot run past the
    /// ceiling it was begun under, so the test is redundant with what the scan does — and stating it
    /// here makes that a property of the region rather than something taken on trust from elsewhere.
    /// </para>
    /// </summary>
    public bool HasRow(int row)
      => row >= 0 && row < _extent.Height && (_bound is null || _bound.HasRow(row));

    /// <summary>
    /// This region, <paramref name="width"/> columns wide, with its bottom edge discovered by
    /// <paramref name="bound"/> rather than measured. Width and bound arrive together because a
    /// discovery settles its width before it reads a row: the region is as wide as the rule looked,
    /// and as tall as nobody knows until something asks.
    /// <para>
    /// The space is untouched — the region still reads through exactly what it read through before,
    /// and only what it admits has changed.
    /// </para>
    /// <para>
    /// <b>The bound's row 0 must be this region's row 0.</b> <see cref="HasRow"/> passes a region-
    /// local row straight through, so a bound discovered over a different starting row would admit
    /// the wrong ones. <see cref="Slice(Offset)"/> is what keeps that true as a region moves down:
    /// it shifts the bound by the same rows it shifts the origin.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="bound"/> is null.</exception>
    /// <exception cref="OutOfBoundsException"><paramref name="width"/> is wider than this region.</exception>
    internal Plane<TSpace> Bounded(IBound bound, int width)
    {
      if (bound is null)
        throw new ArgumentNullException(nameof(bound));

      // A width past the edge is a bounds condition; a negative one is an argument bug, which is
      // what Area says about it a line later — the same division the constructor draws.
      if (width > Width)
        throw new OutOfBoundsException();

      return new Plane<TSpace>(Space, Origin, new Area(width, _extent.Height), bound);
    }

    /// <summary>
    /// The region <paramref name="offset"/> into this one and <paramref name="area"/> big. The
    /// origin composes, so a slice of a slice is one translation and not two hops.
    /// <para>
    /// A named rectangle is a measured one: the result carries no discovered bottom edge, because
    /// asking for part of a region is not a question about the whole of it. The rows asked for are
    /// admitted one at a time through <see cref="HasRow"/>, so slicing a region still being
    /// discovered reads no further than the slice reaches.
    /// </para>
    /// </summary>
    /// <exception cref="OutOfBoundsException">The requested rectangle does not fit inside this region.</exception>
    public Plane<TSpace> Slice(Offset offset, Area area)
    {
      // Both checks avoid adding the two terms, which can overflow — see the constructor. The
      // second one refuses the wrap outright rather than asking about the wrapped row: a region
      // reaching that far is past any edge there could be, and asking would settle a bound merely
      // to hear so.
      if (offset.Width > Width - area.Width || offset.Height > int.MaxValue - area.Height)
        throw new OutOfBoundsException();

      var rows = offset.Height + area.Height;

      if (rows > 0 && !HasRow(rows - 1))
        throw new OutOfBoundsException();

      return new Plane<TSpace>(Space, Origin + offset, area, null);
    }

    /// <summary>
    /// Everything from <paramref name="offset"/> to the far edge — a starting point with no area to
    /// declare, and the one slice that keeps an unsettled bottom edge unsettled.
    /// </summary>
    /// <exception cref="OutOfBoundsException"><paramref name="offset"/> lies outside this region.</exception>
    public Plane<TSpace> Slice(Offset offset)
    {
      // Checked before the subtraction below, and never by asking how tall this region is. Without
      // the check an oversized offset produces a negative extent, which Size reports as an argument
      // bug rather than as the bounds condition a declaration may recover from; with a forced Area
      // it would settle a bound merely to be told the offset does not fit.
      if (offset.Width > Width || (offset.Height > 0 && !HasRow(offset.Height - 1)))
        throw new OutOfBoundsException();

      var width = Width - offset.Width;

      // The ceiling moves down with the origin on both paths, or a bounded tail would claim more
      // rows below its own origin than its space has — the invariant the public constructor checks.
      // The subtraction is safe: the check above admitted row offset.Height - 1, and nothing is
      // admitted past the space's own height.
      var remaining = new Area(width, _extent.Height - offset.Height);

      return _bound is null
        ? new Plane<TSpace>(Space, Origin + offset, remaining, null)
        : new Plane<TSpace>(Space, Origin + offset, remaining, _bound.Shift(offset.Height));
    }

    /// <summary>
    /// The same region, named over the canonical surface alone — how a region reaches the strategy
    /// calculus, which asks only the four questions every space answers.
    /// <para>
    /// A copy of three fields and a reference: nothing is read, the discovered bottom edge rides
    /// along, and the space is the same object, so a read through the result is the read it would
    /// have been. Erasure is one-way, and it happens once per strategy call rather than once per
    /// cell.
    /// </para>
    /// </summary>
    internal Plane<ISpace> Erased() => new Plane<ISpace>(Space, Origin, _extent, _bound);

    /// <summary>
    /// The same region, named over <typeparamref name="TOther"/> — the way back from the canonical
    /// surface for a caller that knows which space it erased.
    /// <para>
    /// The mirror of <see cref="Erased"/>, and the same cost: a copy of three fields and a
    /// reference, nothing read, the discovered bottom edge riding along. The space is the same
    /// object, so a read through the result is the read it would have been; a space that is not a
    /// <typeparamref name="TOther"/> throws <see cref="InvalidCastException"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="TOther">The space to name this region over.</typeparam>
    /// <exception cref="InvalidCastException">This region's space is not a <typeparamref name="TOther"/>.</exception>
    internal Plane<TOther> Retyped<TOther>()
      where TOther : class, ISpace
      => new Plane<TOther>((TOther)(object)Space, Origin, _extent, _bound);

    /// <summary>
    /// The leading <paramref name="width"/> columns, with everything else about the region left
    /// alone — including a bottom edge still being discovered, which is what makes this different
    /// from slicing to a rectangle: narrowing asks nothing about the height.
    /// </summary>
    /// <exception cref="OutOfBoundsException"><paramref name="width"/> is wider than this region.</exception>
    internal Plane<TSpace> Narrowed(int width)
    {
      // A width past the edge is a bounds condition; a negative one is an argument bug, which is
      // what Area says about it a line later — the same division the constructor draws.
      if (width > Width)
        throw new OutOfBoundsException();

      return new Plane<TSpace>(Space, Origin, new Area(width, _extent.Height), _bound);
    }

    /// <summary><paramref name="area"/>, from this region's own corner.</summary>
    /// <exception cref="OutOfBoundsException"><paramref name="area"/> does not fit inside this region.</exception>
    public Plane<TSpace> Slice(Area area) => Slice(default, area);

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>, counted from this plane's own
    /// corner. A row past a discovered bottom edge is an ordinary overrun, decided one row at a
    /// time.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside this region.</exception>
    public Point<TSpace> this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Width || !HasRow(row))
          throw new OutOfBoundsException();

        return new Point<TSpace>(Space, Origin.Width + column, Origin.Height + row);
      }
    }

    /// <summary>
    /// Whether <paramref name="other"/> names the same region of the same space, discovered the same
    /// way. Two regions still being discovered are equal only when they share a bound — comparing
    /// them by extent would have to settle both, and a comparison must not read a file.
    /// </summary>
    public bool Equals(Plane<TSpace> other)
      => ReferenceEquals(Space, other.Space)
        && ReferenceEquals(_bound, other._bound)
        && Same(Origin.Size, other.Origin.Size)
        && Same(_extent.Size, other._extent.Size);

    /// <summary>Equality against any object — see <see cref="Equals(Plane{TSpace})"/> when the other value is a plane.</summary>
    public override bool Equals(object? obj) => obj is Plane<TSpace> other && Equals(other);

    /// <summary>
    /// Consistent with <see cref="Equals(Plane{TSpace})"/>: the space's identity hash mixed with the
    /// origin and the extent, so a space that hashes itself by value cannot make two regions
    /// collide.
    /// </summary>
    public override int GetHashCode()
      => Hashes.Combine(
        RuntimeHelpers.GetHashCode(Space),
        Hashes.Combine(
          Hashes.Combine(Origin.Width, Origin.Height),
          Hashes.Combine(_extent.Width, _extent.Height)));

    /// <summary>Same as <see cref="Equals(Plane{TSpace})"/>.</summary>
    public static bool operator ==(Plane<TSpace> first, Plane<TSpace> second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Plane<TSpace> first, Plane<TSpace> second) => !(first == second);

    /// <summary>
    /// The region as <c>(column,row) WxH</c>, in the space's own coordinates, with an undiscovered
    /// bottom edge written <c>?</c> rather than settled — rendering a region must not read a file.
    /// For diagnostics: a plane knows where it sits in its space and not where that space sits in a
    /// workbook, so this is never an A1 address.
    /// </summary>
    public override string ToString()
      => $"({Origin.Width},{Origin.Height}) {Width}x{(_bound is null ? _extent.Height.ToString() : "?")}";

    private static bool Same(Size first, Size second)
      => first.Width == second.Width && first.Height == second.Height;
  }
}
