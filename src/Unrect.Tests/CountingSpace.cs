using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// A sheet that remembers what was read through it, so a test can assert on the reading itself
  /// rather than only on the answer.
  /// <para>
  /// Two claims need this and cannot be made any other way. A strategy that "settles early" is
  /// making a statement about cells it did NOT read, and an answer is the same either way. And a
  /// bound that is discovered as a projection consumes it is one whose rows are touched in step with
  /// the projection, which is a claim about <em>when</em>, not about what.
  /// </para>
  /// <para>
  /// A region is arithmetic over this space, so every read arrives in this space's own coordinates
  /// and there is nothing to translate: a row is counted under the number the outermost space calls
  /// it because that is the only number there is.
  /// </para>
  /// </summary>
  internal sealed class CountingSpace : ISheetCells
  {
    private readonly ISheetCells _inner;
    private readonly HashSet<int> _rows = new HashSet<int>();

    public CountingSpace(ISheetCells inner) => _inner = inner;

    /// <summary>How many cells have been read through this space.</summary>
    public int CellReads { get; private set; }

    /// <summary>How many distinct rows have been touched.</summary>
    public int RowsTouched => _rows.Count;

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
    public CellKind KindAt(int column, int row) => _inner.KindAt(column, Read(row));

    /// <inheritdoc/>
    public string Describe(int column, int row) => _inner.Describe(column, Read(row));

    /// <inheritdoc/>
    public bool IsErrorAt(int column, int row) => _inner.IsErrorAt(column, Read(row));

    /// <inheritdoc/>
    public string? ErrorTextAt(int column, int row) => _inner.ErrorTextAt(column, Read(row));

    /// <summary>
    /// Records one cell read and hands the row straight back, so every member counts by using its
    /// argument rather than by remembering to.
    /// </summary>
    private int Read(int row)
    {
      CellReads++;
      _rows.Add(row);

      return row;
    }
  }
}
