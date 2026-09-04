using System.Globalization;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The retention family's fixture: a text-heavy ledger sheet, in flavours that differ in exactly one
  /// property each — how many DISTINCT text values the same number of cells hold, and (for the eager
  /// door) how the file spells them.
  ///
  /// <para><b>Why a second fixture at all</b>, rather than reusing the streaming family's. That one is
  /// a kind-mix built to make a parse pay what a real parse pays; two of its eight columns are text
  /// and the strings are short. Retention is measured against a change that dedups strings, so the
  /// fixture has to be one where strings are most of the live set — otherwise the floor moves by a
  /// rounding error and the trend line says nothing. Changing the streaming fixture to serve both
  /// would re-baseline that family's whole history, which is a worse trade than a second fixture.</para>
  ///
  /// <para><b>One door gets a real file and the other does not, and the asymmetry is the point.</b> The
  /// change this family judges lives in the ADAPTERS. The streaming door's adapter seam is
  /// <c>SheetStore</c>'s chunk fill, which every <c>IRowSource</c> passes through — including
  /// <see cref="LedgerRowSource"/> — so a synthetic source exercises the real seam, and does it in
  /// milliseconds. The eager door's seam is inside <c>SpreadsheetSpace.Create</c>, which a
  /// <c>GridSpace</c> built here would bypass completely: those rows would read flat under the very
  /// change they are supposed to be the floor for. So <see cref="EagerSpace"/> reads a genuine
  /// <c>.xlsx</c>, generated at setup by <see cref="RetentionWorkbooks"/> from these same cells.</para>
  ///
  /// <para><b>Numbers are doubles here, unlike the streaming family's fixture.</b> A real reader hands
  /// the adapter a <c>double</c> for every numeric cell, so <c>CellValue.Of(decimal)</c> would put a
  /// boxed decimal in the fixture's cells that no real read produces — 16 MB of it at this size, in a
  /// measurement whose whole subject is retained bytes. (<c>StreamingSpaces</c> is deliberately left
  /// alone: it measures durations, where the box is noise, and changing it would re-baseline that
  /// family's history.)</para>
  ///
  /// <para><b>What is NOT measured, on either door:</b> ExcelDataReader's shared-string table. The
  /// eager door drops it when <c>Create</c> returns, and the streaming door never builds one here.
  /// On a real text-heavy sheet held open it is itself a large retained object that no window bounds
  /// (<c>Workbook</c>'s "the floor, said out loud"). These numbers are the grid's and the projection's
  /// retention, not the process's.</para>
  ///
  /// <para><b>The one property the fixture must have, and the reason for the odd formatting.</b> Nothing
  /// here may hand out a cached instance: every string is built by concatenation at the moment the cell
  /// is made, so what reaches an adapter is what a reader's would be. The indices are formatted to a
  /// fixed six digits so that the duplicated and the unique flavour hold strings of IDENTICAL length and
  /// count, and differ only in how many distinct values those strings spell. That is what makes a
  /// <c>_Unique</c> row a control rather than a second measurement: today a pair must measure the same,
  /// and a change that dedups strings must move the duplicated row and leave its control flat.</para>
  /// </summary>
  internal static class RetentionSpaces
  {
    /// <summary>
    /// Rows per sheet, the streaming family's size for the same reason: eight columns of it is two
    /// million cells, which is large enough that the retained set is tens of megabytes (so a
    /// megabyte of measurement residue cannot be mistaken for a result) and small enough that a
    /// scenario builds in well under a second, which matters when the job builds each of them four
    /// times.
    /// </summary>
    public const int Rows = 250_000;

    public const int Columns = 8;

    /// <summary>The default window, in rows — the same default a <c>Workbook</c> would use.</summary>
    public const int WindowRows = 8192;

    /// <summary>
    /// Distinct values per text column in the duplicated flavour. Chosen to span the range real
    /// sheets actually show: a currency code repeats tens of thousands of times, a client identifier
    /// a few hundred, and a reference number never repeats at all. The reference column is the
    /// incompressible part on purpose — a dedup that appeared to remove it would be measuring
    /// something other than dedup.
    /// </summary>
    public const int DistinctClients = 5_000;

    /// <inheritdoc cref="DistinctClients"/>
    public const int DistinctDescriptions = 250;

    /// <inheritdoc cref="DistinctClients"/>
    public const int DistinctRegions = 12;

    /// <inheritdoc cref="DistinctClients"/>
    public const int DistinctCurrencies = 3;

    /// <summary>Row 0 carries the captions <see cref="LedgerRow"/> binds against.</summary>
    private static readonly string[] Captions =
      { "Client", "Region", "Description", "Reference", "Amount", "Units", "Settled", "Currency" };

    /// <summary>
    /// The cell at a coordinate, defined once so the eager and streaming fixtures cannot drift: the
    /// two doors' rows are only worth reading against each other, and a difference between two
    /// different sheets means nothing.
    /// </summary>
    public static CellValue Cell(int column, int row, bool unique)
    {
      if (row == 0)
        return CellValue.Of(Captions[column]);

      return column switch
      {
        0 => CellValue.Of(Text("C", row, DistinctClients, unique)),
        1 => CellValue.Of(Text("Region-", row, DistinctRegions, unique)),
        2 => CellValue.Of(Text("Capital call for period ", row, DistinctDescriptions, unique)),
        // Unique in BOTH flavours: the part of a real sheet no dedup can touch.
        3 => CellValue.Of(Text("TX-", row, Rows, unique: true)),
        // Doubles, because that is what a reader yields — see the type's note. Both land on quarters
        // and whole numbers, so they survive the file round trip exactly and bind to `decimal` and
        // `int` members without a conversion failure.
        4 => CellValue.Of(row / 4d),
        5 => CellValue.Of((double)(row % 97)),
        6 => CellValue.Of(row % 2 == 0),
        _ => CellValue.Of(Text("CUR", row, DistinctCurrencies, unique)),
      };
    }

    /// <summary>
    /// One text cell's value, freshly allocated every call — see the type's note on why nothing here
    /// may hand back a cached instance. Six fixed digits either way, so the two flavours differ in
    /// distinct values and in nothing else.
    /// </summary>
    private static string Text(string prefix, int row, int period, bool unique) =>
      prefix + (unique ? row : row % period).ToString("D6", CultureInfo.InvariantCulture);

    /// <summary>
    /// <para>The eager door's product, through the eager door: a real <c>.xlsx</c> of these cells, read by
    /// <c>SpreadsheetSpace.Create</c>. Every string in the returned grid was materialised by
    /// ExcelDataReader and adapted by <c>GetCellValue</c> — which is the seam the interning change
    /// will land on, and the reason this row cannot be a locally-built grid.
    /// </para>
    /// <para>
    /// <paramref name="sharedStrings"/> is how the file spells its text, which is what decides whether
    /// the reader duplicates it at all — see <see cref="RetentionWorkbooks"/> for the measurements.
    /// False (inline) is the floor's case: a fresh instance per cell. True is the other end, where the
    /// reader hands back one instance per distinct value and there is nothing left to intern.
    /// </para>
    /// </summary>
    public static ISpace EagerSpace(bool unique, bool sharedStrings, int rows = Rows, int columns = Columns) =>
      SpreadsheetSpace.Create(
        RetentionWorkbooks.Path(unique, sharedStrings, rows, columns),
        RetentionWorkbooks.SheetName);

    /// <summary>A pool over a fresh synthetic ledger source.</summary>
    public static ReaderPool Pool(bool unique, int maxReaders = 3, int rows = Rows, int columns = Columns)
    {
      var pool = new ReaderPool(new LedgerRowSource(rows, columns, unique), maxReaders, warmReaders: false);

      // What a workbook does at Open: park a reader and adopt it as the first lease.
      pool.Adopt(pool.OpenParked(), 0, 0);

      return pool;
    }

    /// <summary>A window over a synthetic ledger sheet, sized in rows.</summary>
    public static ISpace Windowed(ReaderPool pool, int windowRows = WindowRows, int rows = Rows, int columns = Columns)
    {
      var chunkRows = SheetStore.DefaultChunkRows(columns);

      return new WindowedSpace(
        new SheetStore(pool, 0, "Data", rows, columns, chunkRows, SheetStore.WindowChunksFor(windowRows, chunkRows)));
    }

    /// <summary>Rows generated on demand: a reader's shape, with none of a reader's cost.</summary>
    private sealed class LedgerRowSource : IRowSource
    {
      private readonly int _rows;
      private readonly int _columns;
      private readonly bool _unique;

      internal LedgerRowSource(int rows, int columns, bool unique)
      {
        _rows = rows;
        _columns = columns;
        _unique = unique;
      }

      public string Name => "synthetic-ledger";

      public IRowCursor Open() => new LedgerRowCursor(_rows, _columns, _unique);
    }

    private sealed class LedgerRowCursor : IRowCursor
    {
      private readonly int _rows;
      private readonly bool _unique;
      private int _row = -1;

      internal LedgerRowCursor(int rows, int columns, bool unique)
      {
        _rows = rows;
        _unique = unique;
        ColumnCount = columns;
      }

      public int SheetIndex => 0;

      public string SheetName => "Data";

      public int RowCount => _rows;

      public int ColumnCount { get; }

      public bool NextSheet() => false;

      public bool Read()
      {
        if (_row + 1 >= _rows)
          return false;

        _row++;

        return true;
      }

      public CellValue this[int column] =>
        column < 0 || column >= ColumnCount ? CellValue.Blank : Cell(column, _row, _unique);

      public void Dispose()
      {
      }
    }
  }

  /// <summary>
  /// The row the retention family's table declaration binds to: five string members, because the
  /// question the family exists to answer is what the strings cost.
  /// </summary>
  public sealed record LedgerRow(
    string Client,
    string Region,
    string Description,
    string Reference,
    decimal Amount,
    int Units,
    bool Settled,
    string Currency);
}
