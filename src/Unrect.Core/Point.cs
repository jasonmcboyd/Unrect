using System;
using System.Runtime.CompilerServices;

namespace Unrect.Core
{
  /// <summary>
  /// One cell of one space, as an address rather than as a value: which space, which column, which
  /// row. Every question asked of it is forwarded to the space — so a point reads whatever its space
  /// reads, and is only as alive as its space is.
  /// <para>
  /// The coordinates are the space's own <b>root</b> coordinates, never a local frame: a space has
  /// one coordinate system, and a locator that named a cell relative to something else would have to
  /// be resolved before it could be read.
  /// </para>
  /// <para>
  /// The 0-dimensional locator, and the smallest one: it names a cell and nothing more. Naming a
  /// region is a larger locator's job, not something a point can be asked to do.
  /// </para>
  /// <para>
  /// <b>Equality is address equality.</b> Two points are equal when they name the same cell of the
  /// same space — the same <see cref="Space"/> instance, the same <see cref="Column"/>, the same
  /// <see cref="Row"/> — and never because two cells happen to hold the same value. <c>a == b</c>
  /// asks whether <c>a</c> and <c>b</c> are the same place, not whether they say the same thing; to
  /// ask the latter, compare what <see cref="AsText"/> gives back.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space this point addresses a cell of.</typeparam>
  public readonly struct Point<TSpace> : IEquatable<Point<TSpace>>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/> of <paramref name="space"/>,
    /// in that space's own root coordinates.
    /// <para>
    /// Minting is a locator's job, and this constructor trusts its caller to have checked the
    /// coordinate. It deliberately does not check for itself: the only thing it could check against
    /// is the space's extent, and asking a space how tall it is can settle a boundary the
    /// declaration was still discovering.
    /// </para>
    /// <para>
    /// The consequence is that <c>default(Point&lt;TSpace&gt;)</c> is a point with no space. It
    /// compares, hashes and prints like any other, and throws <see cref="System.NullReferenceException"/>
    /// on any read — a bug in the reading code rather than a condition a declaration recovers from,
    /// which is what a point with nothing to read is.
    /// </para>
    /// </summary>
    public Point(TSpace space, int column, int row)
    {
      Space = space;
      Column = column;
      Row = row;
    }

    /// <summary>The space this point addresses a cell of, and reads through.</summary>
    public TSpace Space { get; }

    /// <summary>The cell's column, 0-based from <see cref="Space"/>'s own origin.</summary>
    public int Column { get; }

    /// <summary>The cell's row, 0-based from <see cref="Space"/>'s own origin.</summary>
    public int Row { get; }

    /// <summary>Whether the cell carries no value at all.</summary>
    public bool IsBlank => Space.IsBlank(Column, Row);

    /// <summary>The negation of <see cref="IsBlank"/>.</summary>
    public bool HasValue => !IsBlank;

    /// <summary>Whether the cell's canonical text is its own value — see <see cref="ISpace.IsText"/>.</summary>
    public bool IsText => Space.IsText(Column, Row);

    /// <summary>
    /// What the cell says, or null when it is blank. A method rather than a property because a cell
    /// that is not text has to be rendered, and rendering may allocate.
    /// </summary>
    public string? AsText() => Space.AsText(Column, Row);

    /// <summary>
    /// The same cell, named over the canonical surface alone — how a point travels in a value that
    /// cannot be generic in the space, such as the exception a failed read throws.
    /// <para>
    /// A copy of a reference and two integers: nothing is read, and the space is the same object, so
    /// a read through the result is the read it would have been. Erasure is one-way.
    /// </para>
    /// </summary>
    public Point<ISpace> Erased() => new Point<ISpace>(Space, Column, Row);

    /// <summary>
    /// The same cell, named over <typeparamref name="TOther"/> — the way back from the canonical
    /// surface for a caller that knows which space it erased.
    /// <para>
    /// The mirror of <see cref="Erased"/>, and the same cost: a copy of a reference and two
    /// integers, nothing read, the same space object underneath. A space that is not a
    /// <typeparamref name="TOther"/> throws <see cref="InvalidCastException"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="TOther">The space to name this cell over.</typeparam>
    /// <exception cref="InvalidCastException">This cell's space is not a <typeparamref name="TOther"/>.</exception>
    internal Point<TOther> Retyped<TOther>()
      where TOther : class, ISpace
      => new Point<TOther>((TOther)(object)Space, Column, Row);

    /// <summary>
    /// Whether <paramref name="other"/> names the same cell of the same space — see the type's own
    /// summary for why this is not a comparison of values.
    /// </summary>
    public bool Equals(Point<TSpace> other)
      => ReferenceEquals(Space, other.Space) && Column == other.Column && Row == other.Row;

    /// <summary>Equality against any object — see <see cref="Equals(Point{TSpace})"/> when the other value is a point.</summary>
    public override bool Equals(object? obj) => obj is Point<TSpace> other && Equals(other);

    /// <summary>
    /// Consistent with <see cref="Equals(Point{TSpace})"/>: the space's identity hash mixed with the
    /// coordinates, so a space that hashes itself by value cannot make two addresses collide.
    /// </summary>
    public override int GetHashCode()
      => Hashes.Combine(RuntimeHelpers.GetHashCode(Space), Hashes.Combine(Column, Row));

    /// <summary>Same as <see cref="Equals(Point{TSpace})"/>.</summary>
    public static bool operator ==(Point<TSpace> first, Point<TSpace> second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Point<TSpace> first, Point<TSpace> second) => !(first == second);

    /// <summary>
    /// The cell's address inside its own space, as <c>(column,row)</c>. For diagnostics: a point
    /// knows where it sits in the space it came from and not where that space sits in the sheet, so
    /// this is never an A1 address.
    /// </summary>
    public override string ToString() => $"({Column},{Row})";
  }
}
