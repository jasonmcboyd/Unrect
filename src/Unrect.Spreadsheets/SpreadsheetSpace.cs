using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The eager door onto a spreadsheet file: one worksheet, read whole, as a space. Reads
  /// <c>.xls</c> and <c>.xlsx</c> through ExcelDataReader and adapts each cell to a
  /// <see cref="CellValue"/> — which is where <em>blankness is decided</em>, the one question the
  /// grid itself cannot answer.
  /// <para>
  /// This is a factory and not a type. What comes back is an <see cref="ISpace"/>, or an
  /// <see cref="ISpreadsheetSpace"/> where formulas were asked for: the domain's face is the
  /// interface, because the eager door, the streaming door and a test double are three different
  /// objects and a declaration should not be written against any one of them. (The delegation shell
  /// this class used to be — a space that forwarded every member to the grid inside it — is gone
  /// with the same reasoning.)
  /// </para>
  /// <para>
  /// <b>Formulas are opt-in, by a second factory rather than a flag.</b>
  /// <see cref="CreateWithFormulas(string, string, bool, Func{CellValue, bool})"/> costs a second
  /// pass over the file's own bytes and hands back a space that carries formulas; the plain
  /// <see cref="Create(string, string, bool, Func{CellValue, bool})"/> pays nothing and hands back
  /// one that does not <em>implement</em> the capability at all. A flag could not do this: the two
  /// answers differ in their type, and a space that implemented <see cref="IFormulaSpace"/> and
  /// answered null everywhere would tell every caller the file has no formulas when the truth is
  /// that nobody looked.
  /// </para>
  /// <para>
  /// <b>Known limitation: the modern Excel errors arrive as blank on the .xlsx path.</b>
  /// ExcelDataReader's XML reader returns null for an error literal it does not recognise <em>and</em>
  /// nulls the value with it, so a <c>#SPILL!</c>, <c>#CALC!</c> or <c>#FIELD!</c> cell reaches this
  /// adapter byte-for-byte identical to an empty one. The information is destroyed upstream; there
  /// is nothing here to detect and no workaround worth attempting.
  /// </para>
  /// <para>
  /// It matters more than an ordinary fidelity gap because blankness is load-bearing:
  /// <c>AfterBlankRows</c>, <c>RowsWhileAnyValue</c> and a repeat's separator all key off it, so a
  /// single such cell in a data column can quietly truncate a region rather than fail loudly. The
  /// <c>.xls</c> path is unaffected — it reports an error code, and an unrecognised one lexes to
  /// <see cref="Unrect.Core.CellError.Other"/> carrying its literal. See
  /// <c>docs/design/vendor-type-survey.md</c> §8.4.
  /// </para>
  /// </summary>
  public static class SpreadsheetSpace
  {
    private static readonly Func<CellValue, bool> WhitespaceIsBlank =
      value => value.TryGetString() is string text && string.IsNullOrWhiteSpace(text);

    /// <summary>
    /// The named sheet of <paramref name="path"/>, with blankness decided by
    /// <paramref name="isBlank"/> — see the sibling overload for what the default does.
    /// </summary>
    public static ISpace Create(
      string path,
      string sheetName,
      bool caseSensitive = false,
      Func<CellValue, bool>? isBlank = null)
      => Sheet(path, sheetName, caseSensitive, isBlank, withFormulas: false);

    /// <summary>
    /// Every sheet of <paramref name="path"/> matching <paramref name="predicate"/>.
    /// <para>
    /// Blankness belongs to the adapter, so <paramref name="isBlank"/> decides which cells count as
    /// empty space for the strategies downstream. The default treats whitespace-only text as blank:
    /// exported workbooks are full of <c>"  "</c> cells that look empty, are meant to be empty, and
    /// would otherwise anchor a region. Pass <c>_ => false</c> for strict fidelity, where only
    /// genuinely absent cells are blank. Fidelity has one floor: the adapter maps absent cells and
    /// empty-string cells to Blank before this predicate runs, so no predicate can distinguish
    /// <c>""</c> from a cell that does not exist.
    /// </para>
    /// <para>
    /// The default cannot blank an error cell, because an error is not text — which is the right
    /// outcome: <c>#REF!</c> is something the sheet says, not empty space to be skipped.
    /// </para>
    /// <para>
    /// A sheet whose reader will not say how big it is is measured by being read, so it comes back as
    /// tall as the rows that arrive and as wide as the widest of them — the same extent
    /// <see cref="Workbook.Sheet"/> gives for the same sheet. It costs those rows in memory while the
    /// grid is built, and only for such a sheet.
    /// </para>
    /// </summary>
    public static IEnumerable<ISpace> Create(
      string path,
      Func<SpreadsheetContext, bool> predicate,
      Func<CellValue, bool>? isBlank = null)
      => Read(path, predicate, isBlank, withFormulas: false);

    /// <summary>
    /// The named sheet of <paramref name="path"/>, carrying the formula behind each cell as well as
    /// its value — <c>.xlsx</c> only.
    /// <para>
    /// The formulas are read from the file's own bytes in a second pass (ExcelDataReader parses the
    /// formula element and drops it, with no public seam to reach the text through), so this costs
    /// one more read of the sheet's XML and one string per formula cell. The plain factory pays
    /// none of it.
    /// </para>
    /// <para>
    /// <b>What a cell answers.</b> The file's own expression, without the leading <c>=</c>, and
    /// null where the cell is a plain value. A <em>shared</em> formula — the master-and-followers
    /// form Excel writes for a filled column, and 55% of the formula cells in this project's
    /// real-workbook corpus — is reconstructed per cell: the master's text with its relative
    /// references shifted to where the follower sits, its absolute ones left alone. An
    /// <em>array</em> formula is spelled once, at its anchor, and the cells it spills into carry no
    /// formula in the file and answer null; a data table carries no expression at all and answers
    /// null likewise. See <c>SharedFormulas</c> for why the two cheaper answers to the shared case
    /// (the master's text everywhere, or null) were both rejected as misreporting.
    /// </para>
    /// <para>
    /// A workbook whose formulas cannot be read fails here rather than coming back silently empty:
    /// a <c>.xls</c> stores formulas as BIFF token streams, which this package does not decompile,
    /// and asking for them throws <see cref="NotSupportedException"/>.
    /// </para>
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="path"/> is not an xlsx.</exception>
    public static ISpreadsheetSpace CreateWithFormulas(
      string path,
      string sheetName,
      bool caseSensitive = false,
      Func<CellValue, bool>? isBlank = null)
      => (ISpreadsheetSpace)Sheet(path, sheetName, caseSensitive, isBlank, withFormulas: true);

    /// <summary>
    /// Every sheet of <paramref name="path"/> matching <paramref name="predicate"/>, carrying
    /// formulas — see <see cref="CreateWithFormulas(string, string, bool, Func{CellValue, bool})"/>
    /// for what is read and <see cref="Create(string, Func{SpreadsheetContext, bool}, Func{CellValue, bool})"/>
    /// for what <paramref name="isBlank"/> decides.
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="path"/> is not an xlsx.</exception>
    public static IEnumerable<ISpreadsheetSpace> CreateWithFormulas(
      string path,
      Func<SpreadsheetContext, bool> predicate,
      Func<CellValue, bool>? isBlank = null)
      // Every space this enumeration yields was built with a formula grid, so the cast is a
      // statement of what the overload above already decided rather than a hope about the elements.
      => Read(path, predicate, isBlank, withFormulas: true).Cast<ISpreadsheetSpace>();

    private static ISpace Sheet(
      string path,
      string sheetName,
      bool caseSensitive,
      Func<CellValue, bool>? isBlank,
      bool withFormulas)
      => Read(
        path,
        c => caseSensitive ? sheetName == c.Name : sheetName.Equals(c.Name, StringComparison.OrdinalIgnoreCase),
        isBlank,
        withFormulas)
      .FirstOrDefault()
      // Named, because "sequence contains no elements" tells a caller nothing about the workbook
      // they opened or the name they asked for.
      ?? throw new ArgumentException($"No sheet named '{sheetName}' in '{path}'.", nameof(sheetName));

    private static IEnumerable<ISpace> Read(
      string path,
      Func<SpreadsheetContext, bool> predicate,
      Func<CellValue, bool>? isBlank,
      bool withFormulas)
    {
      SpreadsheetEncodings.Register();

      var blank = isBlank ?? WhitespaceIsBlank;

      // One table for the whole call rather than one per sheet: captions and codes repeat across the
      // sheets of a workbook, so a caller enumerating several of them gets one instance per distinct
      // value across all of them. It is dropped when the enumeration ends.
      var texts = new TextTable();

      // FileShare.ReadWrite: the workbook may be open in Excel (which holds a write handle), and
      // concurrent readers of the same file must not block each other. FileShare.Delete: Excel
      // saves by writing a temporary file and replacing the original, which an open read handle
      // would otherwise block. A workbook replaced mid-read surfaces as a zip or CRC read failure
      // from the reader below, not as silently wrong cells.
      using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      // Auto-detect format, supports:
      //  - Binary Excel files (2.0-2003 format; *.xls)
      //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
      using var reader = ExcelReaderFactory.CreateReader(stream);
      // A second handle on the same file, and only when formulas were asked for: the value reader
      // consumes its stream forwards, and the formulas live in a different part of the zip.
      using var formulas = withFormulas ? XlsxFormulas.Open(path) : null;

      var sheetIndex = -1;
      do
      {
        sheetIndex++;
        var context = new SpreadsheetContext(sheetIndex, reader.Name);

        if (!predicate(context))
          continue;

        // A sheet that declines to report its extent is measured by reading it — the same answer the
        // streaming door gives, rather than the empty space a grid sized from nothing would be.
        var cells = reader.RowCount > 0
          ? ReadDeclared(reader, blank, texts)
          : ReadMeasured(reader, blank, texts);

        var values = new GridSpace(cells);

        yield return formulas is null
          ? values
          : new SpreadsheetGridSpace(
            values,
            formulas.ReadSheet(reader.Name, sheetIndex, cells.GetLength(1), cells.GetLength(0)));

      } while (reader.NextResult());
    }

    /// <summary>
    /// The sheet at the size the reader gave, filled row by row. A sheet that yields fewer rows or
    /// narrower ones than it claimed keeps the size it claimed; the cells nothing reached are blank.
    /// </summary>
    private static CellValue[,] ReadDeclared(IExcelDataReader reader, Func<CellValue, bool> blank, TextTable texts)
    {
      var rowCount = reader.RowCount;
      var fieldCount = reader.FieldCount;

      // Already blank: default(CellValue) is Blank, so a short row leaves the cells it never
      // reached exactly as they should be, with no fill pass over the sheet.
      var cells = new CellValue[rowCount, fieldCount];

      var row = 0;
      while (row < rowCount && reader.Read())
      {
        var columnCount = Math.Min(fieldCount, reader.FieldCount);
        for (int i = 0; i < columnCount; i++)
          cells[row, i] = Adapt(reader, i, blank, texts);

        row++;
      }

      return cells;
    }

    /// <summary>
    /// The sheet as reading it turns out to be — for a reader that will not say how big it is, which
    /// is what a sheet with no <c>dimension</c> element and no valued cell to infer one from amounts
    /// to.
    /// <para>
    /// The extent is the one the streaming door measures for the same sheet: as tall as the rows that
    /// actually arrive, as wide as the widest of them, and blank where a shorter row ran out. Sizing
    /// a grid from a count of nothing instead would yield an empty space for a sheet with rows in it,
    /// and say nothing about having done so — which is the one outcome an adapter must not have.
    /// </para>
    /// <para>
    /// Each row is asked its own width rather than the sheet's, because a reader that was never told
    /// the extent may only learn it as rows go past. It costs the rows in memory, which the declared
    /// path does not: nothing here knows how tall the sheet is until it ends. (No real file can
    /// exercise a per-row-varying width today — the only reader state that reaches this path also
    /// reports every row zero wide — so the rule is forward-proofing mirrored from
    /// <c>Workbook.Measure</c>; the width-learned-from-rows behaviour is pinned through the streaming
    /// door's fakes. The caveat is recorded in full in <c>SpreadsheetSpaceTests</c>.)
    /// </para>
    /// </summary>
    private static CellValue[,] ReadMeasured(IExcelDataReader reader, Func<CellValue, bool> blank, TextTable texts)
    {
      var rows = new List<CellValue[]>();
      var width = 0;

      while (reader.Read())
      {
        var values = new CellValue[reader.FieldCount];
        for (int i = 0; i < values.Length; i++)
          values[i] = Adapt(reader, i, blank, texts);

        rows.Add(values);
        width = Math.Max(width, values.Length);
      }

      var cells = new CellValue[rows.Count, width];
      for (int row = 0; row < rows.Count; row++)
        for (int column = 0; column < rows[row].Length; column++)
          cells[row, column] = rows[row][column];

      return cells;
    }

    /// <summary>
    /// One cell of the reader's current row, canonical — which is where blankness is decided, and
    /// where repeated text is given one instance to share. Shared by both fill paths, so a sheet that
    /// reported its extent and one that had to be measured cannot disagree about what a cell is.
    /// </summary>
    private static CellValue Adapt(IExcelDataReader reader, int column, Func<CellValue, bool> blank, TextTable texts)
    {
      var value = reader.GetCellValue(column);

      return blank(value) ? CellValue.Blank : texts.Share(value);
    }

    /// <summary>
    /// The eager door's find-my-twin table: one canonical instance per distinct string, so equal
    /// <c>Text</c> cells in the grid point at the same characters instead of holding a copy each. A
    /// file that spells its text inline hands this adapter a fresh instance per cell, and on a
    /// text-heavy sheet the copies are most of what the grid retains.
    /// <para>
    /// Deliberately simpler than the streaming door's <see cref="StringInterner"/>, which it mirrors
    /// in behaviour and not in machinery. A fill here is single-threaded, so a plain
    /// <see cref="Dictionary{TKey, TValue}"/> does what a concurrent dictionary would at less cost;
    /// and it carries no cap, because it is scoped to the <c>Create</c> <em>call</em> and dies with
    /// it. (A <see cref="HashSet{T}"/> is the honest structure for a set of canonical instances and
    /// is what this was; <c>HashSet&lt;T&gt;.TryGetValue</c> — the one member that makes a set usable
    /// as an intern table — does not exist on netstandard2.0, so the key and the value are the same
    /// string here.) That scope
    /// is the trade, stated plainly: a single-sheet <c>Create</c> disposes its enumerator at the
    /// sheet it wanted, so the table holds nothing past the grid it filled — but a <c>foreach</c>
    /// over several sheets holds one reference per distinct value of every sheet materialised so
    /// far, whether the caller kept those grids or not, until the enumeration ends. That is
    /// cross-sheet sharing, which is the point of one table per call rather than one per sheet, and
    /// it is what an unbounded table costs to get it. What it does keep is the same length guard, so
    /// for any file whose distinct text fits the streaming door's cap the two doors share exactly the
    /// same values and a caller cannot tell which one produced a grid.
    /// </para>
    /// </summary>
    private sealed class TextTable
    {
      private readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.Ordinal);

      internal CellValue Share(CellValue value)
      {
        if (value.TryGetString() is not string text || text.Length > StringInterner.MaximumLength)
          return value;

        // A hit costs one lookup and a first sighting two, which is the right way round: the cells
        // this exists for are the repeats.
        if (_texts.TryGetValue(text, out var canonical))
          return CellValue.Of(canonical);

        _texts.Add(text, text);

        return value;
      }
    }
  }
}
