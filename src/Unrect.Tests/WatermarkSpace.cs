using System;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// A space that remembers how far down the sheet the reading has got, and how far back behind that
  /// it ever reached. A forward pass holds every row from the oldest one a machine may still read,
  /// so how far back a declaration reaches is what the pass costs — and this is the instrument that
  /// measures it with no streaming machinery involved, and therefore nothing to argue with.
  /// <para>
  /// It is deliberately coarser than <see cref="CountingSpace"/> and answers a different question.
  /// That one counts what was read (how many cells, how many distinct rows); this one records the
  /// <em>order</em>: a declaration that reads every row once but reaches back for an earlier one
  /// holds the rows between, and no count can tell you that.
  /// </para>
  /// <para>
  /// A region is arithmetic over this space, so every read arrives in this space's own coordinates
  /// and there is nothing to translate.
  /// </para>
  /// </summary>
  internal sealed class WatermarkSpace : ICellSpace
  {
    private readonly ICellSpace _inner;
    private readonly Trace _trace = new Trace();

    public WatermarkSpace(ICellSpace inner) => _inner = inner;

    /// <summary>The deepest a read ever fell behind the furthest row read so far, in rows.</summary>
    public int BackwardReach => _trace.BackwardReach;

    /// <summary>The furthest row read.</summary>
    public int HighWaterMark => _trace.HighWaterMark;

    /// <inheritdoc/>
    public Area Area => _inner.Area;

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => _inner.IsBlank(column, Read(row));

    /// <inheritdoc/>
    public string? AsText(int column, int row) => _inner.AsText(column, Read(row));

    /// <inheritdoc/>
    public bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem)
      => _inner.TryGetTextAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetDoubleAt(int column, int row, out double value, out CellProblem? problem)
      => _inner.TryGetDoubleAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetDateTimeAt(int column, int row, out DateTime value, out CellProblem? problem)
      => _inner.TryGetDateTimeAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetBooleanAt(int column, int row, out bool value, out CellProblem? problem)
      => _inner.TryGetBooleanAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetErrorAt(int column, int row, out string error) => _inner.TryGetErrorAt(column, Read(row), out error);

    /// <summary>
    /// Records one row touched and hands it straight back, so every member traces by using its
    /// argument rather than by remembering to.
    /// </summary>
    private int Read(int row)
    {
      _trace.Touch(row);

      return row;
    }

    private sealed class Trace
    {
      public int HighWaterMark { get; private set; } = -1;

      public int BackwardReach { get; private set; }

      public void Touch(int row)
      {
        if (row > HighWaterMark)
          HighWaterMark = row;
        else if (HighWaterMark - row > BackwardReach)
          BackwardReach = HighWaterMark - row;
      }
    }
  }
}
