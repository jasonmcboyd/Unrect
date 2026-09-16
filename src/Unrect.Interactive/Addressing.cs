using System;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Interactive
{
  /// <summary>
  /// Reaching one cell by hand, for a script that is looking at a sheet rather than declaring one.
  /// <para>
  /// <b>What this package is for.</b> <c>Unrect.Interactive</c> is the LINQPad-and-notebook half of
  /// the vocabulary: sugar for exploring a file you have not described yet — poking at a cell,
  /// printing a corner, scaffolding a first record from a header row. Nothing in the library uses
  /// any of it, and nothing in it participates in a declaration. A projection that ships is written
  /// against <c>Unrect.Projections</c>; what is here is how you find out what to write.
  /// </para>
  /// <para>
  /// An address is not a declaration. Naming a cell by its coordinates is exactly the index
  /// arithmetic the projection vocabulary exists to remove, so reaching for <c>At</c> inside a shape
  /// is a smell — it is for the line above the shape, where you are still asking what the file holds.
  /// </para>
  /// </summary>
  public static class Addressing
  {
    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/> of <paramref name="space"/>, in
    /// the space's own 0-based coordinates — the whole sheet read as one plane, which is what checks
    /// the coordinate.
    /// </summary>
    /// <typeparam name="TSpace">The space being read.</typeparam>
    /// <param name="space">The space to address a cell of.</param>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> is null.</exception>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside the space.</exception>
    public static Point<TSpace> At<TSpace>(this TSpace space, int column, int row)
      where TSpace : class, ISpace
      => Plane<TSpace>.Of(space)[column, row];

    /// <summary>
    /// The cell <paramref name="address"/> names, written the way the file writes it — <c>B4</c>,
    /// <c>AB7</c>, letters in either case.
    /// <para>
    /// A <c>$</c>-locked address (<c>$A$1</c>) is refused rather than tolerated: locking is a
    /// formula's business — it says what happens when a reference is copied — and nothing is being
    /// copied here, so accepting it would be accepting a word this method has no meaning for.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space being read.</typeparam>
    /// <param name="space">The space to address a cell of.</param>
    /// <param name="address">An A1 cell address.</param>
    /// <exception cref="ArgumentNullException"><paramref name="space"/> or <paramref name="address"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="address"/> does not name a cell.</exception>
    /// <exception cref="OutOfBoundsException">The cell lies outside the space.</exception>
    public static Point<TSpace> At<TSpace>(this TSpace space, string address)
      where TSpace : class, ISpace
    {
      if (address is null)
        throw new ArgumentNullException(nameof(address));

      if (!A1Reference.TryParse(address, out var column, out var row))
        throw new ArgumentException($"'{address}' is not an A1 cell address.", nameof(address));

      return space.At(column, row);
    }
  }
}
