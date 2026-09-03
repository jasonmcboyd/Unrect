using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// Somewhere rows can be read from, forward only, more than once. The streaming store is written
  /// against this rather than against ExcelDataReader, for three reasons that are all load-bearing:
  /// <list type="number">
  ///   <item>
  ///     <b>Blankness stays at the adapter.</b> An implementation produces <see cref="CellValue"/>s
  ///     with the blankness predicate already applied, so the store never sees the predicate. That
  ///     is "blankness is decided at adaptation time", the rule the eager path already honours.
  ///   </item>
  ///   <item>
  ///     <b>The store becomes testable.</b> Chunk maths, eviction, pool selection, warming races and
  ///     above all an IO failure at a chosen row are impossible to arrange with a real workbook and
  ///     trivial with a synthetic source.
  ///   </item>
  ///   <item>
  ///     <b>The benchmarks can exist.</b> CI runners get no workbooks, so the streaming family has to
  ///     measure a synthetic source or not run at all.
  ///   </item>
  /// </list>
  /// <para>
  /// Internal on purpose. Publishing it — CSV, a database cursor, Parquet — is the obvious follow-on
  /// and is deliberately deferred: making it public commits to an API before a second implementation
  /// has argued with it.
  /// </para>
  /// </summary>
  internal interface IRowSource
  {
    /// <summary>What this source is, for diagnostics — a file path, for the spreadsheet adapter.</summary>
    string Name { get; }

    /// <summary>
    /// Opens an independent forward-only cursor over the whole workbook, positioned before the first
    /// row of sheet 0.
    /// <para>
    /// <b>Expensive.</b> On the spreadsheet adapter this is the ~5s open — CPU-bound, because it is
    /// the shared-string table parse — and it is the cost the reader pool exists to overlap and
    /// avoid. Treat every call as a real expense.
    /// </para>
    /// </summary>
    IRowCursor Open();
  }

  /// <summary>
  /// One forward-only position in a workbook: a sheet, and a row within it. Mirrors the shape of the
  /// underlying reader so the store's load loop is the loop it would have written anyway.
  /// <para>
  /// A cursor moves forward and never back — to an earlier row, or an earlier sheet, there is no
  /// route but a new <see cref="IRowSource.Open"/>. That single constraint is the whole reason the
  /// pool, the window and their statistics exist.
  /// </para>
  /// </summary>
  internal interface IRowCursor : IDisposable
  {
    /// <summary>The zero-based index of the sheet this cursor is on.</summary>
    int SheetIndex { get; }

    /// <summary>The name of the sheet this cursor is on.</summary>
    string SheetName { get; }

    /// <summary>
    /// Rows in the current sheet, or a non-positive number when the source cannot say — some xlsx
    /// files carry no <c>dimension</c> element. The store rejects that case loudly rather than
    /// truncating a sheet silently.
    /// </summary>
    int RowCount { get; }

    /// <summary>Columns in the current sheet.</summary>
    int ColumnCount { get; }

    /// <summary>Moves to the next sheet. Forward only; false when there are no more.</summary>
    bool NextSheet();

    /// <summary>Advances one row of the current sheet. False at the end of the sheet.</summary>
    bool Read();

    /// <summary>
    /// A cell of the current row, with blankness <em>already applied</em>. A column past the end of
    /// this row reads as <see cref="CellValue.Blank"/> — a short row is missing cells, not an error.
    /// </summary>
    CellValue this[int column] { get; }
  }
}
