using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// A space that remembers how far down the sheet the reading has got, and how far back behind that
  /// it ever reached. A windowed reader is only as cheap as the walk over it is monotone, so this is
  /// the instrument that says whether a declaration would survive one — with no streaming machinery
  /// involved, and therefore nothing to argue with.
  /// <para>
  /// It is deliberately coarser than <see cref="CountingSpace"/> and answers a different question.
  /// That one counts what was read (how many cells, how many distinct rows); this one records the
  /// <em>order</em>, which is the only thing a window cares about: a declaration that reads every row
  /// once but reads them out of order costs a reload, and no count can tell you that.
  /// </para>
  /// <para>
  /// A region is arithmetic over this space, so every read arrives in this space's own coordinates
  /// and there is nothing to translate. Mirrored from the typed-spaces spike, whose scenario 9 first
  /// measured this.
  /// </para>
  /// </summary>
  internal sealed class WatermarkSpace : ISheetCells
  {
    private readonly ISheetCells _inner;
    private readonly Trace _trace = new Trace();

    public WatermarkSpace(ISheetCells inner) => _inner = inner;

    /// <summary>The deepest a read ever fell behind the furthest row read so far, in rows.</summary>
    public int BackwardReach => _trace.BackwardReach;

    /// <summary>The furthest row read.</summary>
    public int HighWaterMark => _trace.HighWaterMark;

    /// <inheritdoc/>
    public Area Area => _inner.Area;

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => _inner.IsBlank(column, Read(row));

    /// <inheritdoc/>
    public bool IsText(int column, int row) => _inner.IsText(column, Read(row));

    /// <inheritdoc/>
    public string? AsText(int column, int row) => _inner.AsText(column, Read(row));

    /// <inheritdoc/>
    public bool TextAt(int column, int row, out string value, out CellProblem? problem)
      => _inner.TextAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool DecimalAt(int column, int row, out decimal value, out CellProblem? problem)
      => _inner.DecimalAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool IntegerAt(int column, int row, out int value, out CellProblem? problem)
      => _inner.IntegerAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool DoubleAt(int column, int row, out double value, out CellProblem? problem)
      => _inner.DoubleAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool DateTimeAt(int column, int row, out DateTime value, out CellProblem? problem)
      => _inner.DateTimeAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public bool BooleanAt(int column, int row, out bool value, out CellProblem? problem)
      => _inner.BooleanAt(column, Read(row), out value, out problem);

    /// <inheritdoc/>
    public string Describe(int column, int row) => _inner.Describe(column, Read(row));

    /// <inheritdoc/>
    public bool IsErrorAt(int column, int row) => _inner.IsErrorAt(column, Read(row));

    /// <inheritdoc/>
    public string? ErrorTextAt(int column, int row) => _inner.ErrorTextAt(column, Read(row));

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
