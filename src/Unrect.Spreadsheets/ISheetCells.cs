using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A space whose cells carry a spreadsheet's kinds: text, numbers, dates, booleans and errors.
  /// Everything a sheet knows about a cell beyond what it says.
  /// <para>
  /// <b>Each read is a try, and a failure is a sentence.</b> A cell of the wrong kind, or a number
  /// that will not fit the CLR type asked for, is a statement about the data — so it comes back as a
  /// <see cref="CellProblem"/> awaiting an address rather than as an exception, and whatever asked
  /// decides where to say it. The two sentences are deliberately different: a kind failure speaks the
  /// document's vocabulary (<c>expected Number at B4, found Text</c>), a conversion failure speaks
  /// the reader's, about a number that is really there (<c>the Number at B4 (1.5) is not a whole
  /// number</c>).
  /// </para>
  /// <para>
  /// It is the domain's face — what a spreadsheet declaration is written over — and an interface
  /// rather than a class on purpose: the eager door, the streaming door and a test double are three
  /// different objects and none of them is a vendor's type.
  /// </para>
  /// </summary>
  public interface ISheetCells : ISpace
  {
    /// <summary>
    /// The cell's number as a <see cref="decimal"/> — the accessor that keeps a spreadsheet's exact
    /// decimal where the file carried one.
    /// </summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool DecimalAt(int column, int row, out decimal value, out CellProblem? problem);

    /// <summary>
    /// The cell's number as a whole 32-bit one. A number that is really there but is fractional or
    /// out of range fails as a conversion, not as a kind — the cell is a number either way.
    /// </summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool IntegerAt(int column, int row, out int value, out CellProblem? problem);

    /// <summary>The cell's number as a <see cref="double"/>.</summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool DoubleAt(int column, int row, out double value, out CellProblem? problem);

    /// <summary>
    /// The cell's date or time, verbatim. The time of day is kept: truncating is the caller's, not
    /// the sheet's.
    /// </summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool DateTimeAt(int column, int row, out DateTime value, out CellProblem? problem);

    /// <summary>The cell's boolean.</summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool BooleanAt(int column, int row, out bool value, out CellProblem? problem);

    /// <summary>
    /// Which kind the cell is, asked rather than asserted: the question a predicate puts to a cell
    /// before deciding anything about it, where the six reads above assert a kind and refuse a cell
    /// that disagrees. It answers for every cell and fails for none.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    CellKind KindAt(int column, int row);

    /// <summary>
    /// What kind of thing the cell is, in the document's own vocabulary — <c>Text</c>,
    /// <c>Number</c>, <c>Blank</c>, or an error's own spelling. What a message says a cell holds
    /// when it is not what was asked for.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    string Describe(int column, int row);

    /// <summary>
    /// Whether the cell carries an error rather than a value — <c>#DIV/0!</c> and its kin. An error
    /// is a condition, not a value a leaf projects, so this is how a declaration asks about one.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    bool IsErrorAt(int column, int row);

    /// <summary>
    /// The file's own spelling of the cell's error, or null where the cell is not one.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    string? ErrorTextAt(int column, int row);
  }
}
