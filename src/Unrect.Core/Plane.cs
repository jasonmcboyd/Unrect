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
    private readonly Size _extent;

    /// <summary>
    /// The region of <paramref name="space"/> starting at <paramref name="origin"/> and
    /// <paramref name="extent"/> big, in that space's own coordinates.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> is null.</exception>
    /// <exception cref="OutOfBoundsException">The region does not fit inside the space.</exception>
    public Plane(TSpace space, Offset origin, Size extent)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var whole = space.Extent;

      // Compared by subtraction rather than by adding the two terms, because the sum can overflow:
      // every component here is non-negative (a Size refuses a negative), so an origin near
      // int.MaxValue plus any extent at all wraps to a negative that passes a "> width" test, and
      // the region would then be built past the edge and fail later as an argument bug — a fault,
      // which no tolerance boundary may absorb — instead of as the bounds condition it is.
      if (origin.Width > whole.Width - extent.Width || origin.Height > whole.Height - extent.Height)
        throw new OutOfBoundsException();

      Space = space;
      Origin = origin;
      _extent = extent;
    }

    /// <summary>A region already known to fit. Private because every way in has just checked what it is about to build.</summary>
    private Plane(TSpace space, Offset origin, Size extent, bool known)
    {
      Space = space;
      Origin = origin;
      _extent = extent;
    }

    /// <summary>The whole of <paramref name="space"/> — the plane a declaration starts from.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> is null.</exception>
    public static Plane<TSpace> Of(TSpace space)
      => space is null
        ? throw new ArgumentNullException(nameof(space))
        : new Plane<TSpace>(space, default, space.Extent, known: true);

    /// <summary>The space this plane names a region of, and reads through.</summary>
    public TSpace Space { get; }

    /// <summary>Where the region starts, in <see cref="Space"/>'s own coordinates.</summary>
    public Offset Origin { get; }

    /// <summary>How wide the region is.</summary>
    public int Width => _extent.Width;

    /// <summary>How tall the region is.</summary>
    public int Height => _extent.Height;

    /// <summary>How big the region is — its width and height, with no position of its own.</summary>
    public Size Extent => _extent;

    /// <summary>Whether the region has a column at <paramref name="column"/> — the one-column question a scan asks as it folds.</summary>
    public bool HasColumn(int column) => column >= 0 && column < _extent.Width;

    /// <summary>Whether the region has a row at <paramref name="row"/> — the one-row question a scan asks as it folds.</summary>
    public bool HasRow(int row) => row >= 0 && row < _extent.Height;

    /// <summary>
    /// The region <paramref name="offset"/> into this one and <paramref name="extent"/> big. The
    /// origin composes, so a slice of a slice is one translation and not two hops.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The requested rectangle does not fit inside this region.</exception>
    public Plane<TSpace> Slice(Offset offset, Size extent)
    {
      // Both checks avoid adding the two terms, which can overflow — see the constructor.
      if (offset.Width > Width - extent.Width || offset.Height > _extent.Height - extent.Height)
        throw new OutOfBoundsException();

      return new Plane<TSpace>(Space, Origin + offset, extent, known: true);
    }

    /// <summary>Everything from <paramref name="offset"/> to the far edge — a starting point with no area to declare.</summary>
    /// <exception cref="OutOfBoundsException"><paramref name="offset"/> lies outside this region.</exception>
    public Plane<TSpace> Slice(Offset offset)
    {
      // Checked before the subtraction below: without the check an oversized offset produces a
      // negative extent, which Size reports as an argument bug rather than as the bounds condition
      // a declaration may recover from.
      if (offset.Width > Width || offset.Height > _extent.Height)
        throw new OutOfBoundsException();

      return new Plane<TSpace>(Space, Origin + offset, new Size(Width - offset.Width, _extent.Height - offset.Height), known: true);
    }

    /// <summary><paramref name="extent"/>, from this region's own corner.</summary>
    /// <exception cref="OutOfBoundsException"><paramref name="extent"/> does not fit inside this region.</exception>
    public Plane<TSpace> Slice(Size extent) => Slice(default, extent);

    /// <summary>
    /// The same region, named over the canonical surface alone — how a region reaches the strategy
    /// calculus, which asks only the questions every space answers. A copy of three fields
    /// and a reference: nothing is read, and the space is the same object, so a read through the
    /// result is the read it would have been.
    /// </summary>
    internal Plane<ISpace> Erased() => new Plane<ISpace>(Space, Origin, _extent, known: true);

    /// <summary>
    /// The same region, named over <typeparamref name="TOther"/> — the way back from the canonical
    /// surface for a caller that knows which space it erased. The mirror of <see cref="Erased"/>,
    /// and the same cost; a space that is not a <typeparamref name="TOther"/> throws
    /// <see cref="InvalidCastException"/>.
    /// </summary>
    /// <typeparam name="TOther">The space to name this region over.</typeparam>
    /// <exception cref="InvalidCastException">This region's space is not a <typeparamref name="TOther"/>.</exception>
    internal Plane<TOther> Retyped<TOther>()
      where TOther : class, ISpace
      => new Plane<TOther>((TOther)(object)Space, Origin, _extent, known: true);

    /// <summary>The cell at <paramref name="column"/>, <paramref name="row"/>, counted from this plane's own corner.</summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside this region.</exception>
    public Point<TSpace> this[int column, int row]
    {
      get
      {
        if (!HasColumn(column) || !HasRow(row))
          throw new OutOfBoundsException();

        return new Point<TSpace>(Space, Origin.Width + column, Origin.Height + row);
      }
    }

    /// <summary>Whether <paramref name="other"/> names the same region of the same space.</summary>
    public bool Equals(Plane<TSpace> other)
      => ReferenceEquals(Space, other.Space)
        && Origin == other.Origin
        && _extent == other._extent;

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
        Hashes.Combine(Origin.GetHashCode(), _extent.GetHashCode()));

    /// <summary>Same as <see cref="Equals(Plane{TSpace})"/>.</summary>
    public static bool operator ==(Plane<TSpace> first, Plane<TSpace> second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Plane<TSpace> first, Plane<TSpace> second) => !(first == second);

    /// <summary>
    /// The region as <c>(column,row) WxH</c>: its origin in the space's own root coordinates, then
    /// its extent. Coordinates rather than A1, because a space need not be a sheet.
    /// </summary>
    public override string ToString() => $"({Origin.Width},{Origin.Height}) {Width}x{_extent.Height}";
  }
}
