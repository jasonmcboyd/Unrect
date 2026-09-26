using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A space whose cells may carry formulas as well as values.
  /// <para>
  /// The capability lives here, in the package that owns the vocabulary, and nothing in
  /// <c>Unrect.Core</c> or <c>Unrect</c> names it. Strategies still decide extents from content; a
  /// formula-aware matcher is a declaration like any other, shipped beside the backend that can
  /// answer it (see <see cref="SpreadsheetProjections"/>).
  /// </para>
  /// <para>
  /// <b>Honest absence.</b> A space implements this only when it actually read formulas. A reader
  /// that skipped them must not implement it and answer null everywhere, because null here means
  /// "that cell is a plain value" — a statement about the file — and a whole grid of them would say
  /// the file has no formulas when nobody looked. That is why the eager door has a second factory
  /// rather than a flag on the first: see
  /// <see cref="SpreadsheetSpace.CreateWithFormulas(string, string, bool, System.Func{string, bool})"/>.
  /// </para>
  /// <para>
  /// <b>Root coordinates, like every other read.</b> The coordinates are the whole sheet's; a region
  /// of it is arithmetic done by a <see cref="Plane{TSpace}"/> rather than a second space, so there
  /// is no slice that could shed the capability or translate away from it.
  /// </para>
  /// </summary>
  public interface IFormulaSpace : ISpace
  {
    /// <summary>
    /// The formula behind the cell at <paramref name="column"/>, <paramref name="row"/>, in this
    /// space's own coordinates; false where the cell is a plain value. A cell with no formula is
    /// not a failed read — most cells have none — so there is no reason to give.
    /// <para>
    /// <b>The file's own spelling</b>, without the leading <c>=</c>: what the sheet stores is the
    /// expression, and that is what comes back. A shared formula's follower answers with the
    /// expression as it applies to <em>that</em> cell — the master's text with its relative
    /// references shifted — because that is the formula the cell has, whatever the bytes economise
    /// on. See <see cref="SpreadsheetSpace"/> for what the xlsx reader does and does not
    /// reconstruct.
    /// </para>
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    /// <param name="formula">The formula, when the answer is true.</param>
    /// <exception cref="OutOfBoundsException">The coordinates are outside this space.</exception>
    bool TryGetFormulaAt(int column, int row, out string formula);
  }
}
