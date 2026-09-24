using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A space whose cells hold what a spreadsheet's store holds: a <see cref="CellValue"/> — text, a
  /// number, a date, a boolean, an error, or nothing — and nothing the store does not. The value
  /// facet over that one sum type, and an implementer writes <c>ValueAt</c> and the three text
  /// questions every space answers; everything a reader asks of a cell is derived from those once,
  /// so no two doors can disagree about a cell.
  /// <para>
  /// The readings — <c>p.Double()</c>, <c>p.IsDate()</c>, <c>p.TryGetBoolean(out …)</c>, the
  /// kinded leaves, the table binder — are extensions over the value (<see cref="PointReads"/>),
  /// which is where the kind vocabulary lives and where a failure is worded: a cell of the wrong
  /// kind is a statement about the data, so it comes back as a <see cref="CellProblem"/> in the
  /// document's vocabulary (<c>expected Number at B4, found Text</c>), never as an exception from
  /// the space. There is no decimal or integer here because there is none in a workbook: those are
  /// conversions over the double, asked for above the space and failing as conversions.
  /// </para>
  /// <para>
  /// It is the domain's face — what a spreadsheet declaration is written over — and a name of its
  /// own rather than a bare <c>IValueSpace&lt;CellValue&gt;</c>, so a file can say what it reads.
  /// The eager door, the streaming door and a test double are three different objects and none of
  /// them is a vendor's type.
  /// </para>
  /// </summary>
  public interface ICellSpace : IValueSpace<CellValue>
  {
  }
}
