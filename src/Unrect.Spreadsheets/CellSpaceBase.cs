using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The text facet of a sheet, written once over its value facet: a door supplies its extent and
  /// <see cref="ValueAt"/>, and whether a cell is blank and what it says both follow from the value
  /// there.
  /// <para>
  /// There is no other way to be a sheet in this package. Two doors answering a question
  /// differently would be the one bug nothing above them could see, so the answers are not a door's
  /// to write.
  /// </para>
  /// </summary>
  public abstract class CellSpaceBase : ICellSpace
  {
    /// <summary>
    /// Not <c>protected</c>: only this package may be a sheet. An externally authored sheet would
    /// be the one way to make two doors say different things about one cell.
    /// </summary>
    private protected CellSpaceBase()
    {
    }

    /// <inheritdoc/>
    public abstract Area Area { get; }

    /// <summary>
    /// The value at <paramref name="column"/>, <paramref name="row"/> in this space's own
    /// coordinates — the one read a door writes. Implementations throw
    /// <see cref="OutOfBoundsException"/> for a coordinate outside <see cref="Area"/>.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    public abstract CellValue ValueAt(int column, int row);

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => ValueAt(column, row).Kind == CellKind.Blank;

    /// <inheritdoc/>
    public string? AsText(int column, int row) => ValueAt(column, row).AsText();

  }
}
