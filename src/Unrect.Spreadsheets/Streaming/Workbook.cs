using System;
using System.Collections.Generic;
using System.Linq;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A spreadsheet file open for reading its sheets forward, one pass at a time. <see cref="Sheet"/>
  /// hands back a sheet as a space the engine drives row by row, holding only what the declaration's
  /// open machines may still read; ask again for another pass. The workbook owns every cursor it
  /// opens and one string table shared by all its sheets, and closes them at <see cref="Dispose"/>.
  /// </summary>
  public sealed class Workbook : IDisposable
  {
    private readonly object _gate = new object();
    private readonly WorkbookOptions _options;
    private readonly IRowSource _source;
    private readonly StringInterner _strings;
    private readonly List<SheetEntry> _catalogue = new List<SheetEntry>();
    private readonly List<StreamedSheet> _streams = new List<StreamedSheet>();
    private readonly Dictionary<string, StreamedSheet> _latest;

    private IRowCursor? _parked;
    private bool _catalogueComplete;
    private bool _disposed;

    private static readonly Func<Cell, bool> WhitespaceIsBlank =
      value => value.TryGetString() is string text && string.IsNullOrWhiteSpace(text);

    private Workbook(string path, IRowSource source, WorkbookOptions options)
    {
      Path = path;
      _options = options;
      _source = source;
      // One table for the book, shared by every sheet it vends — see StringInterner for why it is
      // scoped there and not per sheet.
      _strings = new StringInterner(options.MaxInternedStrings);
      _latest = new Dictionary<string, StreamedSheet>(
        options.CaseSensitiveSheetNames ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

      // Exactly one file open, parked at sheet 0 and neither walked nor thrown away. Sheet(name)
      // walks it forward to the sheet actually asked for and then adopts it as that sheet's own
      // cursor — already open, already in the right place — which is why the common single-sheet
      // case costs one open rather than two.
      _parked = source.Open();
    }

    /// <summary>Opens <paramref name="path"/> with the default options.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static Workbook Open(string path) => Open(path, new WorkbookOptions());

    /// <summary>Opens <paramref name="path"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range; see <see cref="WorkbookOptions"/>.</exception>
    public static Workbook Open(string path, WorkbookOptions options)
    {
      if (path is null)
        throw new ArgumentNullException(nameof(path));

      if (options is null)
        throw new ArgumentNullException(nameof(options));

      options.Validate();

      return new Workbook(path, new SpreadsheetRowSource(path, options.IsBlank ?? WhitespaceIsBlank), options);
    }

    internal static Workbook Over(IRowSource source, WorkbookOptions options)
    {
      options.Validate();

      return new Workbook(source.Name, source, options);
    }

    /// <summary>The file this workbook reads.</summary>
    public string Path { get; }

    /// <summary>Every sheet's name, in the file's order. Walks the file once to learn them.</summary>
    /// <exception cref="ObjectDisposedException">This workbook has been disposed.</exception>
    public IReadOnlyList<string> SheetNames
    {
      get
      {
        lock (_gate)
        {
          ThrowIfDisposed();
          WalkTo(null);

          return _catalogue.Select(entry => entry.Name).ToArray();
        }
      }
    }

    /// <summary>
    /// The named sheet, as one forward pass over its own cursor. The engine drives it a row at a
    /// time and holds only what the declaration's open machines may still read, up to
    /// <see cref="WorkbookOptions.BufferRows"/>; a cell read directly is loaded on the way to it,
    /// and a cell of a row the pass has released is a located read failure. Each call is a fresh
    /// pass: ask again to read the sheet again.
    /// <para>
    /// A sheet whose reader will not say how big it is — for the formats ExcelDataReader handles
    /// that means a sheet with no valued cell, such as a formatted-but-empty export region — is
    /// measured first, by being read once; <see cref="Statistics"/> reports the rows that cost.
    /// </para>
    /// </summary>
    /// <param name="name">The sheet's name.</param>
    /// <exception cref="ArgumentException">No sheet of that name exists.</exception>
    /// <exception cref="ObjectDisposedException">This workbook has been disposed.</exception>
    public ICellSpace Sheet(string name)
    {
      if (name is null)
        throw new ArgumentNullException(nameof(name));

      lock (_gate)
      {
        ThrowIfDisposed();

        var entry = WalkTo(name)
          ?? throw new ArgumentException(
            $"No sheet named '{name}' in '{Path}'. Sheets seen so far: {Seen()}.", nameof(name));

        var surveyed = entry.RowCount <= 0;
        var (rowCount, columnCount) = surveyed ? Measure(entry) : (entry.RowCount, entry.ColumnCount);

        // The parked cursor is standing on this very sheet at row 0: hand it over rather than
        // closing it and opening another.
        IRowCursor cursor;

        if (_parked is not null && _parked.SheetIndex == entry.Index)
        {
          cursor = _parked;
          _parked = null;
        }
        else
        {
          cursor = OpenAt(entry.Index);
        }

        try
        {
          var stream = new StreamedSheet(cursor, _strings, entry.Name, rowCount, columnCount, _options.BufferRows, surveyed ? rowCount : 0);
          _streams.Add(stream);
          _latest[entry.Name] = stream;
          return stream;
        }
        catch
        {
          cursor.Dispose();
          throw;
        }
      }
    }

    /// <summary>
    /// What the most recent pass over <paramref name="sheetName"/> has cost so far, or null when
    /// that sheet has not been asked for.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="sheetName"/> is null.</exception>
    public StreamingStatistics? Statistics(string sheetName)
    {
      if (sheetName is null)
        throw new ArgumentNullException(nameof(sheetName));

      lock (_gate)
        return _latest.TryGetValue(sheetName, out var stream) ? stream.Statistics : (StreamingStatistics?)null;
    }

    /// <summary>What sharing repeated text has earned this workbook, across every sheet it has read.</summary>
    public InterningStatistics InterningStatistics => _strings.Snapshot();

    private IRowCursor OpenAt(int sheetIndex)
    {
      var cursor = _source.Open();

      try
      {
        for (var index = 0; index < sheetIndex; index++)
          if (!cursor.NextSheet())
            throw new InvalidOperationException($"The file no longer has a sheet at index {sheetIndex}.");

        return cursor;
      }
      catch
      {
        cursor.Dispose();
        throw;
      }
    }

    private (int RowCount, int ColumnCount) Measure(SheetEntry entry)
    {
      using var cursor = OpenAt(entry.Index);

      var rows = 0;
      var columns = entry.ColumnCount;

      while (cursor.Read())
      {
        rows++;
        columns = Math.Max(columns, cursor.ColumnCount);
      }

      return (rows, columns);
    }

    /// <summary>
    /// The catalogue entry for <paramref name="name"/>, walking the file's sheets forward as far as
    /// it takes to find it — or all of them, for null — and remembering every one passed on the way.
    /// The parked cursor does the walking while there is one; otherwise any fresh cursor will do,
    /// since the walk only steps sheets, never rows.
    /// </summary>
    private SheetEntry? WalkTo(string? name)
    {
      var comparison = _options.CaseSensitiveSheetNames ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

      var known = _catalogue.FirstOrDefault(entry => string.Equals(entry.Name, name, comparison));
      if (known is not null || _catalogueComplete)
        return known;

      var fresh = _parked is null;
      var cursor = _parked ?? _source.Open();

      try
      {
        while (true)
        {
          if (cursor.SheetIndex == _catalogue.Count)
          {
            var entry = new SheetEntry(cursor.SheetIndex, cursor.SheetName, cursor.RowCount, cursor.ColumnCount);
            _catalogue.Add(entry);

            if (name is not null && string.Equals(entry.Name, name, comparison))
              return entry;
          }

          if (!cursor.NextSheet())
          {
            _catalogueComplete = true;
            RetireParked();

            return null;
          }
        }
      }
      finally
      {
        if (fresh)
          cursor.Dispose();
      }
    }

    private void RetireParked()
    {
      _parked?.Dispose();
      _parked = null;
    }

    private string Seen() =>
      _catalogue.Count == 0 ? "(none)" : string.Join(", ", _catalogue.Select(entry => $"'{entry.Name}'"));

    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(Workbook), $"The workbook '{Path}' has been disposed.");
    }

    /// <summary>Closes every cursor this workbook opened. A sheet read after this says the workbook is gone.</summary>
    public void Dispose()
    {
      lock (_gate)
      {
        if (_disposed)
          return;

        _disposed = true;

        foreach (var stream in _streams)
          stream.Dispose();

        _streams.Clear();

        // The table outlives a pass by design, so it must not outlive the workbook: a caller who
        // holds a disposed book to total up what an import cost would otherwise still be pinning
        // every distinct string of it. The counters survive; the strings do not.
        _strings.Release();

        RetireParked();
      }
    }

    private sealed class SheetEntry
    {
      internal SheetEntry(int index, string name, int rowCount, int columnCount)
      {
        Index = index;
        Name = name;
        RowCount = rowCount;
        ColumnCount = columnCount;
      }

      internal int Index { get; }

      internal string Name { get; }

      internal int RowCount { get; }

      internal int ColumnCount { get; }
    }
  }
}
