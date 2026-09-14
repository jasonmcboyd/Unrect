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
  /// hundred struct copies.
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

      if (origin.Width + area.Width > extent.Width || origin.Height + area.Height > extent.Height)
        throw new OutOfBoundsException();

      Space = space;
      Origin = origin;
      Area = area;
    }

    /// <summary>The whole of <paramref name="space"/> — the plane a declaration starts from.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> is null.</exception>
    public static Plane<TSpace> Of(TSpace space)
      => space is null
        ? throw new ArgumentNullException(nameof(space))
        : new Plane<TSpace>(space, default, space.Area);

    /// <summary>The space this plane names a region of, and reads through.</summary>
    public TSpace Space { get; }

    /// <summary>Where the region starts, in <see cref="Space"/>'s own root coordinates.</summary>
    public Offset Origin { get; }

    /// <summary>How big the region is.</summary>
    public Area Area { get; }

    /// <summary>
    /// The region <paramref name="offset"/> into this one and <paramref name="area"/> big. The
    /// origin composes, so a slice of a slice is one translation and not two hops — and what comes
    /// back is a plane over the same space, never a weaker one.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The requested rectangle does not fit inside <see cref="Area"/>.</exception>
    public Plane<TSpace> Slice(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Area.Width || offset.Height + area.Height > Area.Height)
        throw new OutOfBoundsException();

      return new Plane<TSpace>(Space, Origin + offset, area);
    }

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>, counted from this plane's own
    /// corner. The point that comes back carries the space's root coordinates, so it names the same
    /// cell whichever plane minted it.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    public Point<TSpace> this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
          throw new OutOfBoundsException();

        return new Point<TSpace>(Space, Origin.Width + column, Origin.Height + row);
      }
    }

    /// <summary>Whether <paramref name="other"/> names the same region of the same space.</summary>
    public bool Equals(Plane<TSpace> other)
      => ReferenceEquals(Space, other.Space)
        && Same(Origin.Size, other.Origin.Size)
        && Same(Area.Size, other.Area.Size);

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
          Hashes.Combine(Area.Width, Area.Height)));

    /// <summary>Same as <see cref="Equals(Plane{TSpace})"/>.</summary>
    public static bool operator ==(Plane<TSpace> first, Plane<TSpace> second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Plane<TSpace> first, Plane<TSpace> second) => !(first == second);

    /// <summary>
    /// The region as <c>(column,row) WxH</c>, in the space's own coordinates. For diagnostics: a
    /// plane knows where it sits in its space and not where that space sits in a workbook, so this
    /// is never an A1 address.
    /// </summary>
    public override string ToString() => $"({Origin.Width},{Origin.Height}) {Area.Width}x{Area.Height}";

    private static bool Same(Size first, Size second)
      => first.Width == second.Width && first.Height == second.Height;
  }
}
