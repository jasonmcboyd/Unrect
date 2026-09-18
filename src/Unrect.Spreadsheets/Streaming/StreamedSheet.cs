using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// One sheet read once, forward, under the push interpreter: rows are loaded as the driver asks
  /// for them and dropped as soon as no open machine may still read them. A cell of a dropped row
  /// is a located read failure — a forward pass cannot go back — and a cell of a row not yet loaded
  /// is a bounds condition. The sheet's extent is what the file declares, so a plane over it can be
  /// cut before its rows have arrived.
  /// </summary>
  internal sealed class StreamedSheet : SheetCellsBase, IRowFeed, IDisposable
  {
    private readonly IRowCursor _cursor;
    private readonly List<Cell[]> _rows = new List<Cell[]>();
    private int _first;
    private bool _exhausted;
    private bool _disposed;

    internal StreamedSheet(IRowCursor cursor, string name, int rowCount, int columnCount, int? cap)
    {
      _cursor = cursor;
      Name = name;
      Area = new Area(columnCount, rowCount);
      Cap = cap;
    }

    internal string Name { get; }

    public override Area Area { get; }

    public int Loaded { get; private set; }

    public int Retained => _rows.Count;

    public int? Cap { get; }

    /// <summary>The most rows held at once over the sheet's life — what the declaration cost.</summary>
    public int PeakRetained { get; private set; }

    public bool Advance()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(Workbook), "the workbook was disposed under the map");

      if (_exhausted || Loaded >= Area.Height || !_cursor.Read())
      {
        _exhausted = true;
        return false;
      }

      var row = new Cell[Area.Width];

      for (var column = 0; column < row.Length; column++)
        row[column] = _cursor[column];

      _rows.Add(row);
      Loaded++;
      PeakRetained = Math.Max(PeakRetained, _rows.Count);
      return true;
    }

    public void Release(int row)
    {
      // The newest row is never released: it is the one being offered.
      var upTo = Math.Min(row, Loaded - 1);

      while (_first < upTo && _rows.Count > 0)
      {
        _rows.RemoveAt(0);
        _first++;
      }
    }

    private protected override Cell CellAt(int column, int row)
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(Workbook), "the workbook that lent this sheet has been disposed");

      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      if (row < _first)
        throw new CellReadException(
          new Point<ISpace>(this, column, row),
          at => $"row {row + 1} of '{Name}' has left the buffer: a streamed sheet is read once, forward, so read {at} inside the projection rather than after it");

      if (row >= Loaded)
        throw new OutOfBoundsException();

      return _rows[row - _first][column];
    }

    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      _rows.Clear();
      _cursor.Dispose();
    }
  }
}
