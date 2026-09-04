using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A declared extent whose height is discovered while it is read rather than measured before the
  /// reading starts — the engine's half of the incremental strategy calculus. The declaration still
  /// decides the boundary: the rule is the strategy's own, written before any data was seen. Only the
  /// moment it runs has moved.
  /// <para>
  /// Forward consumption streams and a dimension query forces: reading a cell advances the scan
  /// through that row and no further, asking for a subspace advances it through the rows asked for,
  /// and asking for <see cref="Area"/> reads the scan to exhaustion. A row below the discovered
  /// bound is an ordinary overrun — <see cref="OutOfBoundsException"/>, exactly as reading past a
  /// measured extent is, which is how a declaration discovers it has run out of room.
  /// </para>
  /// <para>
  /// A scan that <em>breaks</em> is a different thing, and it is the placement's failure rather than
  /// the projection's: <see cref="ShapeEngine"/> hands each bound the failure its own <c>TryPlace</c>
  /// would have thrown for this declaration and this space, so a deferred failure carries the same
  /// subject, path, location and fault flag as the eager one.
  /// </para>
  /// <para>
  /// The scan's position is the one piece of mutable state, and it belongs to a single decomposition
  /// the way a <see cref="DiagnosticCollector"/> does: a bound is built per placement, inside one
  /// <c>Map</c> call, and is never shared across calls.
  /// </para>
  /// </summary>
  internal sealed class BoundedSpace : ISpace
  {
    /// <summary>Rows the scan has accepted — the height so far, and the final one once the scan stops.</summary>
    private int _resolved;

    /// <summary>Whether the scan has stopped, at which point <see cref="_resolved"/> is the height.</summary>
    private bool _stopped;

    public BoundedSpace(ISpace inner, IAreaScan scan, Func<Exception, ShapeException> failure)
    {
      Inner = inner;
      Scan = scan;
      Failure = failure;
    }

    /// <summary>The space this extent is being discovered inside — already resolved for the offset.</summary>
    private ISpace Inner { get; }

    private IAreaScan Scan { get; }

    /// <summary>The placement failure this bound was deferred from, given whatever broke the scan.</summary>
    private Func<Exception, ShapeException> Failure { get; }

    /// <summary>
    /// The extent, which means reading the scan to exhaustion. An <see cref="Core.Area"/> is a pair
    /// of numbers, so there is no answering half of it: the width alone is settled and free, but only
    /// to a caller holding the scan — <see cref="WidthOf"/>, which the views use and no public caller
    /// can. That is why a strategy that must not force reads cells rather than extents —
    /// <c>TakeWhileAnyColumnStrategy</c> is written row-major for exactly this reason.
    /// </summary>
    public Area Area => new Area(ForceResolved());

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        // A row below the discovered bound is outside this extent, which is an overrun like any
        // other. What the scan has not reached yet is decided here, one row at a time.
        if (column < 0 || column >= Scan.Width || row < 0 || !Includes(row))
          throw new OutOfBoundsException();

        return Inner[column, row];
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Scan.Width)
        throw new OutOfBoundsException();

      var rows = offset.Height + area.Height;

      // Through the rows asked for and no further: a request for part of the extent is not a question
      // about the whole of it. What comes back is an ordinary subspace — its extent was just named.
      if (rows > 0 && !Includes(rows - 1))
        throw new OutOfBoundsException();

      return Inner.GetSubspace(offset, area);
    }

    /// <summary>
    /// The width of <paramref name="space"/>, without settling its height — the half of
    /// <see cref="Area"/> that is free, asked for on its own. A discovered bound answers from its
    /// scan, which fixed the width before it read a row; every other space answers from its extent,
    /// where the question was never expensive.
    /// <para>
    /// This and <see cref="HasRow"/> are the seam the views read a bound through, and it is internal
    /// on purpose: <see cref="ISpace"/> hands out one <see cref="Core.Area"/> struct, so a public
    /// caller asking for a width asks for a height too, and no addition here changes that. What it
    /// buys is that <c>CellBlock.Width</c>, <c>TableView.ColumnCount</c> and the column half of every
    /// index check cost nothing on an extent still being discovered.
    /// </para>
    /// </summary>
    internal static int WidthOf(ISpace space)
      => space is BoundedSpace bound ? bound.Scan.Width : space.Area.Width;

    /// <summary>
    /// Whether <paramref name="space"/> has a row at <paramref name="row"/> — the question a
    /// forward-only reader asks instead of "how tall are you". A discovered bound advances its scan
    /// only as far as it takes to answer; every other space compares against its measured height.
    /// <para>
    /// A false answer from a bound means the scan stopped at or before that row, so the height is
    /// settled by the time anything needs to say what it was.
    /// </para>
    /// </summary>
    internal static bool HasRow(ISpace space, int row)
      => row >= 0 && (space is BoundedSpace bound ? bound.Includes(row) : row < space.Area.Height);

    /// <summary>
    /// The extent's size with the scan read to exhaustion — what the engine consumes for a declared
    /// area, since a declared area is consumed in full whether or not the projection used all of it.
    /// Idempotent, and free once anything has settled the bound.
    /// </summary>
    internal Size ForceResolved()
    {
      // One past the last row there could be, so the loop always ends with the scan stopped: its own
      // rule ends it, or running out of rows does.
      Advance(Inner.Area.Height);

      return new Size(Scan.Width, _resolved);
    }

    /// <summary>Whether <paramref name="row"/> is inside the extent, advancing the scan only as far as it takes to say.</summary>
    private bool Includes(int row)
    {
      Advance(row);

      return row < _resolved;
    }

    private void Advance(int throughRow)
    {
      while (!_stopped && _resolved <= throughRow)
      {
        if (_resolved < Inner.Area.Height && IncludesRow(_resolved))
          _resolved++;
        else
          _stopped = true;
      }
    }

    /// <summary>
    /// The scan's own rule, wearing the placement's failure identity. The catch mirrors
    /// <c>TryPlace</c>'s around an area strategy, because it is the same code: a strategy that breaks
    /// while the projection consumes broke for the same reason it would have broken up front.
    /// </summary>
    private bool IncludesRow(int row)
    {
      try
      {
        return Scan.IncludesRow(Inner, row);
      }
      catch (ShapeException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw Failure(exception);
      }
    }
  }
}
