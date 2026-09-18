using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What can be <em>done</em> with a projection here that cannot be done anywhere else: applied to
  /// a sheet of a file, through the streaming door, in one call.
  /// <para>
  /// The split is the one the projection layer already makes — <c>Projection</c> is what a
  /// declaration says and <c>ProjectionExtensions</c> is what is done to it, and this is that
  /// second half for spreadsheets, beside <see cref="SpreadsheetProjections"/>. Like every other
  /// extension in the library it is reached by importing its namespace
  /// (<c>using Unrect.Spreadsheets;</c>), exactly as <c>Map</c> is reached by importing
  /// <c>Unrect.Projections</c>; the backend <em>vocabulary</em> needs no namespace import at all.
  /// </para>
  /// </summary>
  public static class SpreadsheetProjectionExtensions
  {
    /// <summary>
    /// Opens <paramref name="path"/>, reads <paramref name="sheetName"/> through it, and closes it
    /// again — the streaming loop's body as one expression:
    /// <code>
    /// var summaries = paths.Select(path =&gt; report.MapWorkbook(path, "Detail")).ToList();
    /// </code>
    /// <para>
    /// It is the <see cref="Workbook"/> idiom with the <c>using</c> written for you, and nothing
    /// else: one open, one sheet, one map, one close, with the peak bounded per file rather than by
    /// the largest file in the run. Everything a workbook can do that this cannot — several sheets
    /// from one open, a warm second map over the same sheet,
    /// <see cref="Workbook.Statistics"/> — is a reason to open the book yourself, which is three
    /// lines and always available.
    /// </para>
    /// <para>
    /// <b>The workbook is gone when this returns</b>, so what comes back must be values. A result
    /// holding a view onto the sheet — the whole-table rung's <c>TableView</c>, a
    /// <c>CellBlock</c> — reads a disposed workbook afterwards and fails with an
    /// <see cref="ObjectDisposedException"/> the engine classes as a fault. Project what you need
    /// inside the declaration; that is what a declaration is for.
    /// </para>
    /// <para>
    /// <b>The receiver is a sheet declaration or a canonical one, and nothing else.</b> A streamed
    /// sheet reads values only — <see cref="Workbook.Sheet"/> hands back a plain
    /// <see cref="ISheetCells"/>, the honest absence — so there is an overload for a declaration
    /// written over <c>ISheetCells</c> and one for a declaration written over the canonical
    /// <see cref="ISpace"/>, which a sheet also is. A declaration over anything else the sheet
    /// cannot be — <see cref="IFormulaSpace"/>, <see cref="ISpreadsheetSpace"/>, a grid of typed
    /// values — matches neither, so <c>formulaReadingProjection.MapWorkbook(…)</c> is a compile
    /// error rather than a run-time fault or a file's formulas quietly read as absent. Read formulas
    /// through the eager door instead — <c>projection.Map(SpreadsheetSpace.CreateWithFormulas(path,
    /// sheet))</c>, which needs no sugar because it has no lifetime to hide.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="path">The workbook file.</param>
    /// <param name="sheetName">The sheet to read, matched as <see cref="WorkbookOptions.CaseSensitiveSheetNames"/> decides.</param>
    /// <param name="options">How the file is read; the defaults where omitted.</param>
    /// <exception cref="ArgumentException">No sheet of that name exists.</exception>
    public static TResult MapWorkbook<TResult>(
      this IProjectionDefinition<ISheetCells, TResult> projection,
      string path,
      string sheetName,
      WorkbookOptions? options = null)
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));

      return Over(path, sheetName, options, projection.Map);
    }

    /// <inheritdoc cref="MapWorkbook{TResult}(IProjectionDefinition{ISheetCells, TResult}, string, string, WorkbookOptions)"/>
    /// <remarks>
    /// The canonical receiver: a declaration written over <see cref="ISpace"/> asks for nothing a
    /// sheet does not answer, so it reads a workbook through the same door — and a projection type
    /// is invariant, so it needs an overload of its own to say so.
    /// </remarks>
    public static TResult MapWorkbook<TResult>(
      this IProjectionDefinition<ISpace, TResult> projection,
      string path,
      string sheetName,
      WorkbookOptions? options = null)
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));

      return Over(path, sheetName, options, projection.Map);
    }

    /// <summary>
    /// <see cref="MapWorkbook{TResult}(IProjectionDefinition{ISheetCells, TResult}, string, string, WorkbookOptions)"/>,
    /// keeping what the decomposition noticed — every tolerance
    /// boundary that absorbed a failure, every alternative a choice passed over, and space the
    /// projection did not describe.
    /// <para>
    /// The pairing is the same one <c>Map</c> and <c>MapWithDiagnostics</c> have everywhere else,
    /// and it earns its place here because a run over a directory is exactly where nobody is
    /// watching: the diagnostics are how a file that parsed but did not add up says so.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="path">The workbook file.</param>
    /// <param name="sheetName">The sheet to read.</param>
    /// <param name="options">How the file is read; the defaults where omitted.</param>
    /// <exception cref="ArgumentException">No sheet of that name exists.</exception>
    public static MapResult<TResult> MapWorkbookWithDiagnostics<TResult>(
      this IProjectionDefinition<ISheetCells, TResult> projection,
      string path,
      string sheetName,
      WorkbookOptions? options = null)
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));

      return Over(path, sheetName, options, projection.MapWithDiagnostics);
    }

    /// <inheritdoc cref="MapWorkbookWithDiagnostics{TResult}(IProjectionDefinition{ISheetCells, TResult}, string, string, WorkbookOptions)"/>
    /// <remarks>
    /// The canonical receiver, for the same reason its plain twin has one.
    /// </remarks>
    public static MapResult<TResult> MapWorkbookWithDiagnostics<TResult>(
      this IProjectionDefinition<ISpace, TResult> projection,
      string path,
      string sheetName,
      WorkbookOptions? options = null)
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));

      return Over(path, sheetName, options, projection.MapWithDiagnostics);
    }

    /// <summary>
    /// One open, one sheet, one read, one close — the shape all four entry points share, with the
    /// read handed in so the book closes after it and never before.
    /// </summary>
    private static TResult Over<TResult>(string path, string sheetName, WorkbookOptions? options, Func<ISheetCells, TResult> read)
    {
      using var book = Workbook.Open(path, options ?? new WorkbookOptions());

      // Under the push interpreter the sheet is a stream — one forward pass, holding only what the
      // declaration's open machines may still read; under pull it is the windowed door.
      return read(ProjectionEngine.Pushing ? book.Stream(sheetName) : book.Sheet(sheetName));
    }
  }
}
