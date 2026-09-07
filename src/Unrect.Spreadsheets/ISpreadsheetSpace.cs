using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A spreadsheet as this package can read it: a space, plus everything a sheet knows about its
  /// own cells beyond their values. It is the domain's face — the thing a declaration says it is
  /// written over — and it is an interface rather than a class on purpose, because the eager door,
  /// the streaming door and a test double are three different objects and none of them is a
  /// vendor's type.
  /// <para>
  /// <b>Demand the narrowest capability you use.</b> A library projection that reads formulas
  /// should say <see cref="IFormulaSpace"/> and nothing more, so it composes with anything that can
  /// answer it; this bundle is for application code, where "a spreadsheet" is the honest
  /// requirement and naming each capability is ceremony.
  /// </para>
  /// <para>
  /// <b>The bundle is a versioning commitment.</b> Every capability listed here is one every
  /// implementor must supply, so adding one — <c>IFormattingSpace</c> is the next — breaks every
  /// backend that implements this interface. That is accepted pre-1.0 and recorded rather than
  /// discovered: the bundle grows as the package's reading of a sheet grows, and a consumer who
  /// does not want to move with it demands a capability instead of the bundle.
  /// </para>
  /// <para>
  /// Not every space this package vends is one of these. The streaming door reads values only, so
  /// <see cref="Workbook.Sheet"/> hands back a plain <see cref="ISpace"/>; that is the honest
  /// absence rule, not an oversight.
  /// </para>
  /// </summary>
  public interface ISpreadsheetSpace : ISpace, IFormulaSpace
  {
  }
}
