using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// Everything an <see cref="ISheetCells"/> answers, written once over one question: what is the
  /// cell at these coordinates. A door supplies its extent and that one read; the canonical four and
  /// the nine kinded answers follow from them.
  /// <para>
  /// There is no other way to be a sheet in this package. Two doors answering a kind differently
  /// would be the one bug nothing above them could see, so the answers are not a door's to write.
  /// </para>
  /// </summary>
  public abstract class SheetCellsBase : ISheetCells
  {
    /// <summary>
    /// Not <c>protected</c>: only this package may be a sheet. The kinded answers below are what
    /// nothing above a door is allowed to disagree about, and an externally authored sheet would be
    /// the one way to make two doors say different things about one cell.
    /// </summary>
    private protected SheetCellsBase()
    {
    }

    /// <inheritdoc/>
    public abstract Area Area { get; }

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/> in this space's own
    /// coordinates. Implementations throw <see cref="OutOfBoundsException"/> for a coordinate
    /// outside <see cref="Area"/>.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    private protected abstract Cell CellAt(int column, int row);

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => CellAt(column, row).IsBlank;

    /// <inheritdoc/>

    /// <inheritdoc/>
    public string? AsText(int column, int row) => CellAt(column, row).AsText();

    /// <inheritdoc/>
    public bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem)
      => CellReading.Text(CellAt(column, row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetDoubleAt(int column, int row, out double value, out CellProblem? problem)
      => CellReading.Double(CellAt(column, row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetDateTimeAt(int column, int row, out DateTime value, out CellProblem? problem)
      => CellReading.DateTime(CellAt(column, row), out value, out problem);

    /// <inheritdoc/>
    public bool TryGetBooleanAt(int column, int row, out bool value, out CellProblem? problem)
      => CellReading.Boolean(CellAt(column, row), out value, out problem);

    /// <inheritdoc/>
    /// <remarks>
    /// Read off <see cref="Cell.AsText"/> rather than off the cell's stored literal: a cell keeps
    /// its literal only where it differs from the canonical spelling, so an error that arrived
    /// spelled exactly as Excel shows it holds none. Answering false there would make this false for
    /// two different reasons — "not an error" and "an error spelled the usual way" — and the
    /// contract has one.
    /// </remarks>
    public bool TryGetErrorAt(int column, int row, out string error)
    {
      var cell = CellAt(column, row);

      error = cell.Kind == CellKind.Error ? cell.AsText()! : null!;
      return error is not null;
    }
  }
}
