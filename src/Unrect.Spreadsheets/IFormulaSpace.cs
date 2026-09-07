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
  /// <see cref="SpreadsheetSpace.CreateWithFormulas(string, string, bool, System.Func{CellValue, bool})"/>.
  /// </para>
  /// <para>
  /// <b>The slicing law.</b> An implementation's subspaces must be capable too, with translated
  /// coordinates: a slice may never invent capability its parent lacked nor shed what its parent
  /// had. Forgetting is safe in the type system and a lie in a space.
  /// </para>
  /// </summary>
  public interface IFormulaSpace : ISpace
  {
    /// <summary>
    /// The formula behind the cell at <paramref name="column"/>, <paramref name="row"/>, in this
    /// space's own coordinates, or null where the cell is a plain value.
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
    /// <exception cref="OutOfBoundsException">The coordinates are outside this space.</exception>
    string? FormulaAt(int column, int row);
  }
}
