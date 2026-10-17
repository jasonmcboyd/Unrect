using System.Collections.Generic;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// A sheet that remembers what was read through it, so a test can assert on the reading itself
  /// rather than only on the answer.
  /// <para>
  /// Two claims need this and cannot be made any other way. A strategy that "settles early" is
  /// making a statement about cells it did NOT read, and an answer is the same either way. And a
  /// declaration that reads in step with a forward pass is one whose rows are touched as the pass
  /// offers them, which is a claim about <em>when</em>, not about what.
  /// </para>
  /// <para>
  /// A region is arithmetic over this space, so every read arrives in this space's own coordinates
  /// and there is nothing to translate: a row is counted under the number the outermost space calls
  /// it because that is the only number there is.
  /// </para>
  /// </summary>
  internal sealed class CountingSpace : ICellSpace
  {
    private readonly ICellSpace _inner;
    private readonly HashSet<int> _rows = new HashSet<int>();

    public CountingSpace(ICellSpace inner) => _inner = inner;

    /// <summary>How many cells have been read through this space.</summary>
    public int CellReads { get; private set; }

    /// <summary>How many distinct rows have been touched.</summary>
    public int RowsTouched => _rows.Count;

    /// <inheritdoc/>
    public Size Extent => _inner.Extent;

    /// <inheritdoc/>
    public bool IsBlankAt(int column, int row) => _inner.IsBlankAt(column, Read(row));

    /// <inheritdoc/>
    public string AsTextAt(int column, int row) => _inner.AsTextAt(column, Read(row));

    public CellValue ValueAt(int column, int row) => _inner.ValueAt(column, Read(row));

    public bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem)
      => _inner.TryGetTextAt(column, Read(row), out value, out problem);

    public bool TryGetDoubleAt(int column, int row, out double value, out CellProblem? problem)
      => _inner.TryGetDoubleAt(column, Read(row), out value, out problem);

    public bool TryGetDateAt(int column, int row, out System.DateTime value, out CellProblem? problem)
      => _inner.TryGetDateAt(column, Read(row), out value, out problem);

    public bool TryGetBooleanAt(int column, int row, out bool value, out CellProblem? problem)
      => _inner.TryGetBooleanAt(column, Read(row), out value, out problem);

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
