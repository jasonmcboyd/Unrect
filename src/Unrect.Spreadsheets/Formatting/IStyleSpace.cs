using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A space whose cells can say what they look like as well as what they hold.
  /// <para>
  /// A capability beside <see cref="IFormulaSpace"/> and under the same two rules. <b>Honest
  /// absence:</b> a space implements this only when it actually read the workbook's formatting — a
  /// default font everywhere would say nothing in the file is coloured when nobody looked. <b>Root
  /// coordinates:</b> the coordinates are the whole sheet's, and a region of it is arithmetic done by
  /// a <see cref="Plane{TSpace}"/>, so no slice can shed the capability.
  /// </para>
  /// <para>
  /// What is reported is what the FILE states on the cell: a colour somebody set, not one a
  /// conditional-formatting rule would paint when the workbook is open. A rule lives in the file as
  /// a rule; what it tests is data, and a declaration reads that instead.
  /// </para>
  /// </summary>
  public interface IStyleSpace : ISpace
  {
    /// <summary>How the text of the cell at <paramref name="column"/>, <paramref name="row"/> is set.</summary>
    /// <exception cref="OutOfBoundsException">The coordinate is outside the space.</exception>
    CellFont FontAt(int column, int row);

    /// <summary>How the cell at <paramref name="column"/>, <paramref name="row"/> is filled.</summary>
    /// <exception cref="OutOfBoundsException">The coordinate is outside the space.</exception>
    CellFill FillAt(int column, int row);
  }
}
