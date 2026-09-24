using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A rectangular array of <see cref="CellValue"/>s viewed as a sheet: the eager door's grid, and what
  /// every adapter ends up holding. A backend reads its own format, produces one array of cells, and
  /// hands it here — so the indexing, the bounds checking and the kinded answers are written once
  /// and every adapter agrees on them.
  /// <para>
  /// The array is indexed <c>[row, column]</c>, the way a 2D array literal reads on the page, while
  /// a space is indexed <c>[column, row]</c>, the way a spreadsheet address does. This type is where
  /// that transposition happens, and it is the reason no adapter has to think about it.
  /// </para>
  /// <para>
  /// Useful directly, not only to adapters: a test or a script with values already in hand can build
  /// one through <see cref="Of(object?[,])"/> and skip the file entirely, and everything above it
  /// behaves exactly as it does over a workbook.
  /// </para>
  /// </summary>
  public sealed class SheetGrid : CellSpaceBase
  {
    private readonly CellValue[,] _cells;

    private SheetGrid(CellValue[,] cells)
    {
      _cells = cells;
      Area = new Area(cells.GetLength(1), cells.GetLength(0));
    }

    /// <inheritdoc/>
    public override Area Area { get; }

    /// <summary>
    /// The whole of <paramref name="cells"/>, as a sheet. Everything is already decided: whatever
    /// produced the array settled what each cell is, including which are blank — and a cell nobody
    /// filled in is already blank, because <c>default</c> is.
    /// </summary>
    /// <param name="cells">The cells, indexed <c>[row, column]</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cells"/> is null.</exception>
    public static SheetGrid Of(CellValue[,] cells)
      => new SheetGrid(cells ?? throw new ArgumentNullException(nameof(cells)));

    /// <summary>
    /// Heterogeneous values, each adapting to the kind its CLR type implies — the array-adapter
    /// equivalent of a real sheet, and the reason a fixture can be written as a literal.
    /// <para>
    /// Null and the empty string are blank; a <see cref="CellValue"/> passes straight through, which is
    /// how a grid carries an error cell — the one kind with no CLR literal to write it as. A CLR
    /// type with no kind here is an error where the grid is built, not a cell that reads as
    /// something surprising.
    /// </para>
    /// </summary>
    /// <param name="values">The cells, indexed <c>[row, column]</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentException">A value has no cell kind.</exception>
    public static SheetGrid Of(object?[,] values)
    {
      if (values is null)
        throw new ArgumentNullException(nameof(values));

      var cells = new CellValue[values.GetLength(0), values.GetLength(1)];

      for (var row = 0; row < values.GetLength(0); row++)
        for (var column = 0; column < values.GetLength(1); column++)
          cells[row, column] = Adapt(values[row, column]);

      return new SheetGrid(cells);
    }

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>, for a sheet of this package
    /// that lays a second layer over this one.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    /// <inheritdoc/>
    public override CellValue ValueAt(int column, int row)
    {
      // OutOfBoundsException and not IndexOutOfRangeException: running off the edge of a space is a
      // statement about the data that a declaration may recover from, where an index bug is on the
      // engine's fault list and would make the overrun unrecoverable.
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return _cells[row, column];
    }

    private static CellValue Adapt(object? value)
      => value switch
      {
        null => CellValue.Blank,
        CellValue cell => cell,
        string text => text.Length == 0 ? CellValue.Blank : CellValue.Of(text),
        int number => CellValue.Of(number),
        long number => CellValue.Of(number),
        double number => CellValue.Of(number),
        decimal number => CellValue.Of((double)number),
        DateTime moment => CellValue.Of(moment),
        bool flag => CellValue.Of(flag),
        _ => throw new ArgumentException($"No cell kind for {value.GetType()}.", nameof(value)),
      };
  }
}
