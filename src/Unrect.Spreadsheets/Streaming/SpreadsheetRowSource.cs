using System;
using System.IO;

using ExcelDataReader;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A spreadsheet file as a row source. Each <see cref="Open"/> is an independent file handle and an
  /// independent reader, which is what lets a workbook hold several passes over one file at once.
  /// </summary>
  internal sealed class SpreadsheetRowSource : IRowSource
  {
    internal SpreadsheetRowSource(string path)
    {
      Name = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IRowCursor Open() => new SpreadsheetRowCursor(Name);
  }

  /// <summary>
  /// One reader over one spreadsheet file, wrapped as a cursor. Owns both the stream and the reader
  /// and disposes them together.
  /// </summary>
  internal sealed class SpreadsheetRowCursor : IRowCursor
  {
    private readonly FileStream _stream;
    private readonly IExcelDataReader _reader;
    internal SpreadsheetRowCursor(string path)
    {
      SpreadsheetEncodings.Register();

      // The same sharing the eager path uses, and for the same reasons: the workbook may be open in
      // Excel (which holds a write handle), concurrent readers of one file must not block each other,
      // and Excel saves by replacing the file, which an open read handle would otherwise block. A
      // workbook replaced mid-read surfaces as a read failure from the reader, not as wrong cells.
      _stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

      try
      {
        _reader = ExcelReaderFactory.CreateReader(_stream);
      }
      catch
      {
        // The reader owns the stream once it exists; until then this cursor does, and a failed
        // construction must not leak the handle.
        _stream.Dispose();
        throw;
      }
    }

    /// <inheritdoc/>
    public int SheetIndex { get; private set; }

    /// <inheritdoc/>
    public string SheetName => _reader.Name;

    /// <inheritdoc/>
    public int RowCount => _reader.RowCount;

    /// <inheritdoc/>
    public int ColumnCount => _reader.FieldCount;

    /// <inheritdoc/>
    public bool NextSheet()
    {
      if (!_reader.NextResult())
        return false;

      SheetIndex++;

      return true;
    }

    /// <inheritdoc/>
    public bool Read() => _reader.Read();

    /// <inheritdoc/>
    public CellValue this[int column]
    {
      get
      {
        // A row shorter than the sheet is missing cells, not broken: read past its end as Blank
        // rather than letting the reader throw. The eager path expresses the same rule as a
        // Math.Min over the row's field count.
        if (column < 0 || column >= _reader.FieldCount)
          return CellValue.Blank;

        return _reader.GetCellValue(column);
      }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      // The mirror of the constructor's care: it disposes the stream if the reader cannot be built,
      // so this releases the stream even if the reader objects on the way down. A file handle held
      // by a failed teardown is the thing that blocks the next open of the same workbook.
      try
      {
        _reader.Dispose();
      }
      finally
      {
        _stream.Dispose();
      }
    }
  }
}
