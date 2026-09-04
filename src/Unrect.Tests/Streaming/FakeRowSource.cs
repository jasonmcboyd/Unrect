using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// A workbook that is not a file: sheets of generated cells, behind the same seam the spreadsheet
  /// adapter sits behind.
  /// <para>
  /// The seam exists so this can exist. Chunk maths, eviction, lease selection, a warming race and
  /// above all an IO failure at a chosen row are impossible to arrange with a real workbook and
  /// trivial here — and an "open" costs nothing, so a test can afford the dozens of them the pool
  /// laws are stated in.
  /// </para>
  /// <para>
  /// It counts opens and closes so a dispose race can be settled by arithmetic rather than by
  /// waiting, and it can gate an open on an event so the race is arranged rather than hoped for.
  /// </para>
  /// </summary>
  internal sealed class FakeRowSource : IRowSource
  {
    private readonly FakeSheet[] _sheets;
    private int _opens;
    private int _closes;
    private int _opensStarted;

    internal FakeRowSource(params FakeSheet[] sheets)
    {
      if (sheets.Length == 0)
        throw new ArgumentException("A source needs at least one sheet.", nameof(sheets));

      _sheets = sheets;
    }

    /// <summary>The single-sheet case, which most tests want: <paramref name="rows"/> × <paramref name="columns"/>.</summary>
    internal static FakeRowSource Of(int rows, int columns, string name = "Data")
      => new FakeRowSource(new FakeSheet(name, rows, columns));

    /// <inheritdoc/>
    public string Name { get; } = "fake://workbook";

    /// <summary>Cursors opened over this source, ever.</summary>
    internal int Opens => Volatile.Read(ref _opens);

    /// <summary>Cursors disposed. A workbook that leaks nothing ends with this equal to <see cref="Opens"/>.</summary>
    internal int Closes => Volatile.Read(ref _closes);

    /// <summary>
    /// Held closed, every <see cref="Open"/> blocks on it. The way a warming race is arranged rather
    /// than timed: the test decides exactly when the background open is allowed to finish.
    /// </summary>
    internal ManualResetEventSlim? OpenGate { get; set; }

    /// <summary>
    /// Set the instant an <see cref="Open"/> begins, before it blocks on the gate. Its partner: the
    /// gate says when an open may finish, this says when one has provably started, and a race is
    /// only arranged when a test can wait for both.
    /// </summary>
    internal ManualResetEventSlim? OpenStarted { get; set; }

    /// <summary>
    /// Opens that have BEGUN, counted as they enter rather than as they finish — so a test holding
    /// the gate can wait until a chosen number of opens are provably in flight at once, which is
    /// what turns a race into an arrangement.
    /// </summary>
    internal int OpensStarted => Volatile.Read(ref _opensStarted);

    /// <summary>
    /// What to throw, and where. The cursor throws when it is asked to read the failing row of the
    /// failing sheet — which is when the store is materialising the chunk that contains it, so the
    /// failure surfaces from whichever cell read triggered that load.
    /// </summary>
    internal Func<Exception>? Fault { get; set; }

    internal int FaultSheet { get; set; }

    internal int FaultRow { get; set; } = -1;

    /// <inheritdoc/>
    public IRowCursor Open()
    {
      Interlocked.Increment(ref _opensStarted);
      OpenStarted?.Set();
      OpenGate?.Wait();

      Interlocked.Increment(ref _opens);

      return new FakeRowCursor(this, _sheets);
    }

    private void Closed() => Interlocked.Increment(ref _closes);

    private sealed class FakeRowCursor : IRowCursor
    {
      private readonly FakeRowSource _source;
      private readonly FakeSheet[] _sheets;
      private int _row = -1;

      internal FakeRowCursor(FakeRowSource source, FakeSheet[] sheets)
      {
        _source = source;
        _sheets = sheets;
      }

      private FakeSheet Sheet => _sheets[SheetIndex];

      public int SheetIndex { get; private set; }

      public string SheetName => Sheet.Name;

      public int RowCount => Sheet.ReportsDimension ? Sheet.RowCount : 0;

      /// <summary>
      /// Zero until a row has been read, on a sheet that reports no dimension: a reader that was
      /// never told how wide a sheet is learns it from the rows going past, and the measuring pass
      /// is written to exactly that.
      /// </summary>
      public int ColumnCount => Sheet.ReportsDimension || _row >= 0 ? Sheet.ColumnCount : 0;

      public bool NextSheet()
      {
        if (SheetIndex + 1 >= _sheets.Length)
          return false;

        SheetIndex++;
        _row = -1;

        return true;
      }

      public bool Read()
      {
        // The source may report more rows than it will yield — a sheet whose dimension overstates
        // its content — which is how the "a short sheet leaves its tail Blank" case is arranged.
        if (_row + 1 >= Sheet.ReadableRows)
          return false;

        _row++;

        if (_source.Fault is Func<Exception> fault && SheetIndex == _source.FaultSheet && _row == _source.FaultRow)
          throw fault();

        return true;
      }

      public CellValue this[int column] =>
        column < 0 || column >= Sheet.ColumnCount ? CellValue.Blank : Sheet.Cell(column, _row);

      public void Dispose() => _source.Closed();
    }
  }

  /// <summary>
  /// One sheet of a <see cref="FakeRowSource"/>: how big it says it is, how much of that it will
  /// actually yield, and what is in each cell.
  /// </summary>
  internal sealed class FakeSheet
  {
    private readonly Func<int, int, CellValue>? _cell;

    /// <summary>
    /// A sheet whose cells are their own coordinates — <c>"c,r"</c> as text — so any test can name
    /// the cell it expects without a table of literals.
    /// </summary>
    internal FakeSheet(string name, int rowCount, int columnCount, int? readableRows = null)
    {
      Name = name;
      RowCount = rowCount;
      ColumnCount = columnCount;
      ReadableRows = readableRows ?? rowCount;
    }

    internal FakeSheet(string name, int rowCount, int columnCount, Func<int, int, CellValue> cell, int? readableRows = null)
      : this(name, rowCount, columnCount, readableRows)
    {
      _cell = cell;
    }

    /// <summary>A sheet spelled out row by row, for the shapes that need real content.</summary>
    internal static FakeSheet Of(string name, params object?[][] rows)
    {
      var columns = rows.Max(row => row.Length);
      var cells = rows;

      return new FakeSheet(
        name,
        rows.Length,
        columns,
        (column, row) => column < cells[row].Length ? ShapeTestSpaces.Adapt(cells[row][column]) : CellValue.Blank);
    }

    internal string Name { get; }

    /// <summary>What the sheet claims, which is what the store indexes on.</summary>
    internal int RowCount { get; }

    internal int ColumnCount { get; }

    /// <summary>What the cursor will actually yield. Equal to <see cref="RowCount"/> unless a test says otherwise.</summary>
    internal int ReadableRows { get; }

    /// <summary>
    /// Whether the cursor will say how big this sheet is. False models the xlsx files that carry no
    /// <c>dimension</c> element: the sheet is exactly as big as it says here, and the reader reports
    /// none of it until rows have gone past.
    /// </summary>
    internal bool ReportsDimension { get; set; } = true;

    internal CellValue Cell(int column, int row)
      => _cell is null ? CellValue.Of($"{column},{row}") : _cell(column, row);
  }
}
