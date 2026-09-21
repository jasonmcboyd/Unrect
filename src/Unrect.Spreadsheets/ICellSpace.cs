using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A space whose cells hold what a spreadsheet's store holds: text, numbers, dates, booleans and
  /// errors — and nothing the store does not. Every number in a workbook is a double, so there is a
  /// double read and no decimal or integer one: those are conversions a reader asks for, and they
  /// live above the space (<see cref="PointReads.Decimal{TSpace}"/>, the table binder), where they
  /// fail as conversions.
  /// <para>
  /// <b>The contract is one try per reading, and everything else is derived.</b> Each
  /// <c>TryGet…At</c> hands back the value or the reason it could not be had, and the point
  /// extensions — <c>IsDouble()</c>, <c>TryGetDouble()</c>, the asserting <c>Double()</c> — are
  /// written once over it, so none of them can disagree with it. Text is read through
  /// <see cref="ISpace.TryGetTextAt"/>, which every space answers and a sheet words in its own
  /// vocabulary.
  /// </para>
  /// <para>
  /// <b>A failure is a sentence about the reading that was asked for.</b> A cell of the wrong kind
  /// is a statement about the data — so it comes back as a <see cref="CellProblem"/> awaiting an
  /// address rather than as an exception, and whatever asked decides where to say it
  /// (<c>expected Number at B4, found Text</c>).
  /// </para>
  /// <para>
  /// It is the domain's face — what a spreadsheet declaration is written over — and an interface
  /// rather than a class on purpose: the eager door, the streaming door and a test double are three
  /// different objects and none of them is a vendor's type.
  /// </para>
  /// </summary>
  public interface ICellSpace : ISpace
  {
    /// <summary>The cell's number, as the double the store holds.</summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool TryGetDoubleAt(int column, int row, out double value, out CellProblem? problem);

    /// <summary>
    /// The cell's date or time, verbatim. The time of day is kept: truncating is the caller's, not
    /// the sheet's.
    /// </summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool TryGetDateTimeAt(int column, int row, out DateTime value, out CellProblem? problem);

    /// <summary>The cell's boolean.</summary>
    /// <inheritdoc cref="ISpace.TryGetTextAt"/>
    bool TryGetBooleanAt(int column, int row, out bool value, out CellProblem? problem);

    /// <summary>
    /// The file's own spelling of the cell's error — <c>#DIV/0!</c> and its kin — where the cell
    /// carries one. An error is a condition, not a value a leaf projects, and its absence is not a
    /// failure, so there is no reason to give.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <param name="error">The error's spelling, when the answer is true.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    bool TryGetErrorAt(int column, int row, out string error);
  }
}
