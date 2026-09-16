using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A declared extent whose height is discovered while it is read rather than measured before the
  /// reading starts — the engine's half of the incremental strategy calculus, and what a
  /// <see cref="Plane{TSpace}"/> carries instead of a settled height.
  /// <para>
  /// The declaration still decides the boundary: the rule is the strategy's own, written before any
  /// data was seen. Only the moment it runs has moved. Forward consumption streams and a dimension
  /// query forces: asking whether there is a row advances the scan through that row and no further,
  /// while asking how tall the region is reads the scan to exhaustion.
  /// </para>
  /// <para>
  /// A scan that <em>breaks</em> is a different thing, and it is the placement's failure rather
  /// than the projection's: <see cref="ProjectionEngine"/> hands each bound the failure its own
  /// <c>TryPlace</c> would have thrown for this declaration and this space, so a deferred failure
  /// carries the same subject, path, location and fault flag as the eager one.
  /// </para>
  /// <para>
  /// The scan's position is the one piece of mutable state, and it belongs to a single
  /// decomposition the way a <see cref="DiagnosticCollector"/> does: a bound is built per
  /// placement, inside one <c>Map</c> call, and is never shared across calls. A shift shares it —
  /// two bounds over one scan is the point, because a region offset into a bounded one is the same
  /// discovery counted from further down.
  /// </para>
  /// </summary>
  internal sealed class Bound : IBound
  {
    /// <summary>Rows the scan has accepted — the height so far, and the final one once the scan stops.</summary>
    private int _resolved;

    /// <summary>Whether the scan has stopped, at which point <see cref="_resolved"/> is the height.</summary>
    private bool _stopped;

    internal Bound(Plane<ISpace> inner, IAreaScan scan, Func<Exception, ProjectionException> failure)
    {
      Inner = inner;
      Scan = scan;
      Failure = failure;
    }

    /// <summary>The region the scan is read against — already resolved for the offset.</summary>
    private Plane<ISpace> Inner { get; }

    private IAreaScan Scan { get; }

    /// <summary>The placement failure this bound was deferred from, given whatever broke the scan.</summary>
    private Func<Exception, ProjectionException> Failure { get; }

    /// <summary>The width the scan settled on before it read a row — what a plane is narrowed to when it takes this bound.</summary>
    internal int Width => Scan.Width;

    /// <inheritdoc/>
    public bool HasRow(int row)
    {
      if (row < 0)
        return false;

      Advance(row);

      return row < _resolved;
    }

    /// <inheritdoc/>
    public int Force()
    {
      // One past the last row there could be, so the loop always ends with the scan stopped: its
      // own rule ends it, or running out of rows does.
      Advance(Inner.Area.Height);

      return _resolved;
    }

    /// <inheritdoc/>
    // Every shift comes from a slice that first admitted the row it steps over, so the accumulated
    // shift is bounded by the space's own height and the addition below cannot overflow.
    public IBound Shift(int rows) => rows == 0 ? this : new Shifted(this, rows);

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
    /// <c>TryPlace</c>'s around an area strategy, because it is the same code: a strategy that
    /// breaks while the projection consumes broke for the same reason it would have broken up
    /// front.
    /// </summary>
    private bool IncludesRow(int row)
    {
      try
      {
        return Scan.IncludesRow(Inner, row);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw Failure(exception);
      }
    }

    /// <summary>
    /// The same discovery, counted from further down. It owns no scan of its own — the rows it
    /// admits are the ones the bound underneath admits, asked with the shift added — so a region
    /// stepped through in pieces still reads each row exactly once.
    /// </summary>
    private sealed class Shifted : IBound
    {
      private readonly IBound _bound;
      private readonly int _rows;

      internal Shifted(IBound bound, int rows)
      {
        _bound = bound;
        _rows = rows;
      }

      public bool HasRow(int row)
      {
        var actual = _rows + row;

        // Both terms are non-negative, so a negative sum has wrapped — and a row that far down is
        // past anything any discovery could admit. Answering from the wrapped number would say yes.
        return actual >= 0 && _bound.HasRow(actual);
      }

      // The rows stepped over were admitted before anything could step over them — a slice checks
      // HasRow(offset - 1) before it shifts — so the settled height is at least the shift and the
      // subtraction never goes below zero.
      public int Force() => _bound.Force() - _rows;

      public IBound Shift(int rows) => rows == 0 ? this : new Shifted(_bound, _rows + rows);
    }
  }
}
