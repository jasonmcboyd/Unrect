using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A spreadsheet file, read a window at a time. Where
  /// <see cref="SpreadsheetSpace.Create(string, string, bool, Func{CellValue, bool})"/> loads a whole
  /// sheet into memory before anything reads it, a workbook loads rows as the shape asks for them and
  /// holds only a window of them at once.
  ///
  /// <code>
  /// using var book = Workbook.Open(path);
  /// var result = projection.Map(book.Sheet("Data"));
  /// </code>
  ///
  /// <para>and the one it is really for — a declaration written once, applied to a directory of
  /// workbooks, with the peak bounded per iteration rather than by the largest file in the run:</para>
  ///
  /// <code>
  /// var report = VerticalFlow(v =&gt; ...);          // one declaration, reused
  ///
  /// foreach (var path in monthlyCloseOfFunds)
  /// {
  ///   using var book = Workbook.Open(path);
  ///   Publish(report.Map(book.Sheet("Detail")));  // bounded memory per iteration
  /// }
  /// </code>
  ///
  /// <para>Shapes are immutable and workbooks are independent, so <c>Parallel.ForEach</c> over that
  /// same loop needs nothing added. Within one workbook, maps over different sheets run in parallel
  /// and maps over one sheet serialise.</para>
  ///
  /// <para><b>Which door to use.</b> The two paths differ in the shape of their cost, never in their
  /// results. Eager reads the file once, entirely, and a second pass over the same rows is free
  /// because it is an array. Streaming reads as the shape asks, and a second pass costs a cheap
  /// rewind if a reader is parked behind it, or a chunk reload if the window has moved on. A monotone
  /// walk down a sheet measured about 35% slower than eager while holding about 2.7× less live
  /// memory; a declaration that sweeps a band taller than its window can be arbitrarily slower. Use
  /// eager when the file fits comfortably in memory, and a workbook when it does not — or when many
  /// files go through one declaration and the peak is what matters.</para>
  ///
  /// <para><b>The floor, said out loud.</b> Streaming bounds the grid, not the process. The reader's
  /// shared-string table owns every string a <c>Text</c> cell points at, is not part of the window
  /// and does not shrink with it; on a text-heavy sheet it can dominate. What streaming removes is
  /// the materialised grid, not the parser.</para>
  ///
  /// <para><b>Lifetime.</b> The workbook owns every file handle, reader and chunk store. A view from
  /// <see cref="Sheet"/> is a value, not a handle: it has no <c>Dispose</c>, it can be sliced and
  /// held freely, and the only thing that invalidates it is this workbook being disposed — after
  /// which reading one throws <see cref="ObjectDisposedException"/>, deterministically, whether or
  /// not the rows happen still to be in memory.</para>
  /// </summary>
  public sealed class Workbook : IDisposable
  {
    private readonly object _gate = new object();
    private readonly WorkbookOptions _options;
    private readonly ReaderPool _pool;
    private readonly List<SheetEntry> _catalogue = new List<SheetEntry>();
    private readonly Dictionary<string, SheetStore> _stores;

    private IRowCursor? _parked;
    private bool _catalogueComplete;
    private bool _disposed;

    private static readonly Func<CellValue, bool> WhitespaceIsBlank =
      value => value.TryGetString() is string text && string.IsNullOrWhiteSpace(text);

    private Workbook(string path, IRowSource source, WorkbookOptions options)
    {
      Path = path;
      _options = options;
      _pool = new ReaderPool(source, options.MaxReaders, options.WarmReaders);
      _stores = new Dictionary<string, SheetStore>(
        options.CaseSensitiveSheetNames ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

      try
      {
        // Exactly one file open, parked at sheet 0 and neither walked nor thrown away. Sheet(name)
        // walks it forward to the sheet actually asked for and then adopts it as that sheet's first
        // reader — already open, already in the right place — which is why the common single-sheet
        // case costs one open rather than two.
        _parked = _pool.OpenParked();
        _pool.BeginWarming();
      }
      catch
      {
        // A missing or locked file is the most ordinary way this fails, and a constructor that
        // throws leaves no one holding the pool: dispose it on the way out rather than leaking a
        // cancellation source and any reader a warmer had already opened.
        _pool.Dispose();
        throw;
      }
    }

    /// <summary>Opens <paramref name="path"/> with the default options.</summary>
    public static Workbook Open(string path) => Open(path, new WorkbookOptions());

    /// <summary>Opens <paramref name="path"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range.</exception>
    public static Workbook Open(string path, WorkbookOptions options)
    {
      if (path is null)
        throw new ArgumentNullException(nameof(path));

      if (options is null)
        throw new ArgumentNullException(nameof(options));

      options.Validate();

      return new Workbook(path, new SpreadsheetRowSource(path, options.IsBlank ?? WhitespaceIsBlank), options);
    }

    /// <summary>
    /// A workbook over an arbitrary row source — the seam the streaming tests read through. Some
    /// conditions cannot be arranged with a file at all: an IO failure at a chosen row, and a sheet
    /// whose reader reports no dimensions.
    /// </summary>
    internal static Workbook Over(IRowSource source, WorkbookOptions options)
    {
      options.Validate();

      return new Workbook(source.Name, source, options);
    }

    /// <summary>The file this workbook reads.</summary>
    public string Path { get; }

    /// <summary>
    /// Every sheet name, in workbook order.
    /// <para>
    /// <b>Costs an open if you ask first.</b> Naming every sheet means walking the parked reader to
    /// the end of the workbook, which leaves it past every sheet and therefore useless as a first
    /// lease; it is retired, and the first <see cref="Sheet"/> call afterwards pays for a reader of
    /// its own. Asking for a sheet by name first, and the catalogue afterwards, costs nothing extra.
    /// </para>
    /// </summary>
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
    /// The named sheet as a space.
    /// <para>
    /// Idempotent: asking twice returns views over one store, so a second declaration mapped over an
    /// already-open book re-pays neither the reader open nor, if the rows are still resident, the
    /// read. That warm reuse is the point of holding a workbook open rather than a sheet.
    /// </para>
    /// <para>
    /// A sheet whose reader will not say how big it is — some xlsx files carry no <c>dimension</c>
    /// element — is measured here, by being read once. See <see cref="Measure"/>. That survey runs
    /// holding the workbook's gate, so vending such a sheet blocks every other <see cref="Sheet"/>
    /// and <see cref="Statistics"/> call on this workbook for as long as the pass takes.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentException">No sheet of that name exists.</exception>
    /// <exception cref="ObjectDisposedException">This workbook has been disposed.</exception>
    public ISpace Sheet(string name)
    {
      if (name is null)
        throw new ArgumentNullException(nameof(name));

      lock (_gate)
      {
        ThrowIfDisposed();

        if (_stores.TryGetValue(name, out var existing))
          return new WindowedSpace(existing);

        var entry = WalkTo(name)
          ?? throw new ArgumentException(
            $"No sheet named '{name}' in '{Path}'. Sheets seen so far: {Seen()}.", nameof(name));

        // The parked reader is standing on this very sheet at row 0. Hand it to the pool as the
        // first lease rather than closing it and opening another — before the measure below, so a
        // sheet that has to be surveyed is surveyed by the reader already in the right place.
        if (_parked is not null && _parked.SheetIndex == entry.Index)
        {
          _pool.Adopt(_parked, entry.Index, 0);
          _parked = null;
        }

        var surveyed = entry.RowCount <= 0;
        var (rowCount, columnCount) = surveyed ? Measure(entry) : (entry.RowCount, entry.ColumnCount);

        var chunkRows = _options.ChunkRows > 0 ? _options.ChunkRows : SheetStore.DefaultChunkRows(columnCount);

        var store = new SheetStore(
          _pool,
          entry.Index,
          entry.Name,
          rowCount,
          columnCount,
          chunkRows,
          SheetStore.WindowChunksFor(_options.WindowRows, chunkRows),
          // What the survey read, so the pass a caller never asked for is visible where its cost is
          // read: zero for a sheet that described itself.
          rowsMeasured: surveyed ? rowCount : 0);

        _stores.Add(entry.Name, store);

        return new WindowedSpace(store);
      }
    }

    /// <summary>
    /// How big <paramref name="entry"/> really is, found by reading it once and counting — for a
    /// sheet whose reader will not say, which is what some xlsx files with no <c>dimension</c>
    /// element amount to. Called holding the gate.
    /// <para>
    /// <b>Why measure rather than guess.</b> A space has to answer <c>Area</c>, so an unmeasured
    /// sheet would have to claim some upper bound instead — and every declaration that scans blank
    /// rows (a repeat's separator, <c>SkipBlankRows</c>, <c>AfterBlankRows</c>) would then walk that
    /// bound to the end of it after the content ran out, and every unconsumed-space diagnostic would
    /// report a sheet that does not exist. The honest extent costs one forward pass, and only for a
    /// sheet that declined to describe itself.
    /// </para>
    /// <para>
    /// It costs time, never memory: rows are counted and dropped, none is materialised. The pass is
    /// a reader movement and shows up as one in <see cref="ReaderStatistics"/>, and the rows it read
    /// are reported as <see cref="StreamingStatistics.RowsMeasured"/> — which is where this cost is
    /// read, since the sheet's other counters describe reading through the window and this pass never
    /// touched it. It leaves its reader at the end of the sheet, so the first chunk load is served by
    /// another reader — or, in a one-reader pool, by a reopen.
    /// </para>
    /// <para>
    /// The width is watched as well as the rows, because a reader that was not told the sheet's
    /// dimensions may not know how wide it is either until rows go past. A source that never says
    /// measures 0 wide, and the sheet reads as empty rather than as wrong.
    /// </para>
    /// </summary>
    private (int RowCount, int ColumnCount) Measure(SheetEntry entry)
    {
      var lease = _pool.Borrow(entry.Index, 0, out _);

      try
      {
        var cursor = lease.Cursor!;
        var rows = 0;
        var columns = entry.ColumnCount;

        while (cursor.Read())
        {
          lease.CountRow();
          rows++;
          columns = Math.Max(columns, cursor.ColumnCount);
        }

        return (rows, columns);
      }
      finally
      {
        _pool.Return(lease);
      }
    }

    /// <summary>
    /// What reading <paramref name="sheetName"/> has cost so far, or null when that sheet has never
    /// been vended — a sheet nobody asked for has no story to tell.
    /// <para>
    /// Readable after <see cref="Dispose"/>, deliberately, where <see cref="Sheet"/> is not: the
    /// numbers describe reading that has already happened, and a caller totalling up what an import
    /// cost should not have to keep the workbook alive to do it.
    /// </para>
    /// </summary>
    public StreamingStatistics? Statistics(string sheetName)
    {
      if (sheetName is null)
        throw new ArgumentNullException(nameof(sheetName));

      lock (_gate)
        return _stores.TryGetValue(sheetName, out var store) ? store.Snapshot() : (StreamingStatistics?)null;
    }

    /// <summary>What this workbook's readers have cost — shared across every sheet of it.</summary>
    public ReaderPoolStatistics ReaderStatistics => _pool.Snapshot();

    /// <summary>
    /// Finds <paramref name="name"/> in the catalogue, extending the catalogue if it has not reached
    /// that far yet. Null <paramref name="name"/> walks to the end. Called holding the gate.
    /// <para>
    /// The catalogue is built as a reader passes each sheet, so a walk stops at the sheet actually
    /// asked for rather than paying for the whole workbook. Walking eagerly at <c>Open</c> would
    /// give better errors and a free <see cref="SheetNames"/>, at the cost of a second multi-second
    /// open in the single-sheet case that is most usage.
    /// </para>
    /// <para>
    /// Any reader can do the walking, which is what keeps later sheets reachable. The parked reader
    /// does it while it is still parked; afterwards the pool serves the walk like any other forward
    /// read — a lease positioned at the catalogue's edge steps forward from there, and the rows it
    /// moves over are counted like every other movement. Walking to a far sheet is a real forward
    /// read and the statistics see it.
    /// </para>
    /// </summary>
    private SheetEntry? WalkTo(string? name)
    {
      var comparison = _options.CaseSensitiveSheetNames ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

      var known = _catalogue.FirstOrDefault(entry => string.Equals(entry.Name, name, comparison));
      if (known is not null || _catalogueComplete)
        return known;

      if (_parked is not null)
        return Extend(name, comparison, _parked.SheetIndex, () => Describe(_parked!), () => _parked!.NextSheet());

      // Any reader will do: the walk only steps sheets, never rows, so asking for a position
      // would turn a free operation into a backward reach and a multi-second open.
      var lease = _pool.BorrowAnywhere();

      try
      {
        return Extend(name, comparison, lease.SheetIndex, () => Describe(lease.Cursor!), lease.NextSheet);
      }
      finally
      {
        _pool.Return(lease);
      }
    }

    /// <summary>
    /// Steps a cursor forward from wherever it stands, recording each sheet it reaches beyond the
    /// catalogue's edge, until <paramref name="name"/> turns up or the workbook runs out.
    /// <para>
    /// A reader can only be standing on a sheet the catalogue already knows — reaching a sheet at
    /// all requires a store for it, and a store requires a catalogue entry — so a walk starts at or
    /// behind the edge and records exactly when it arrives there. Recording only at
    /// <c>index == Count</c> keeps sheet index and list position the same thing.
    /// </para>
    /// </summary>
    private SheetEntry? Extend(
      string? name,
      StringComparison comparison,
      int startSheetIndex,
      Func<SheetEntry> describe,
      Func<bool> nextSheet)
    {
      var index = startSheetIndex;

      while (true)
      {
        if (index == _catalogue.Count)
        {
          var entry = describe();
          _catalogue.Add(entry);

          if (name is not null && string.Equals(entry.Name, name, comparison))
            return entry;
        }

        if (!nextSheet())
        {
          _catalogueComplete = true;
          RetireParked();

          return null;
        }

        index++;
      }
    }

    private static SheetEntry Describe(IRowCursor cursor) =>
      new SheetEntry(cursor.SheetIndex, cursor.SheetName, cursor.RowCount, cursor.ColumnCount);

    /// <summary>
    /// Lets go of the parked reader once it has been walked past every sheet: it can never be a
    /// first lease now, and the slot it was holding is wanted by ordinary readers.
    /// </summary>
    private void RetireParked()
    {
      if (_parked is null)
        return;

      _parked.Dispose();
      _parked = null;
      _pool.ReleaseParked();
    }

    private string Seen() =>
      _catalogue.Count == 0 ? "(none)" : string.Join(", ", _catalogue.Select(entry => $"'{entry.Name}'"));

    private void ThrowIfDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException(nameof(Workbook), $"The workbook '{Path}' has been disposed.");
    }

    /// <summary>
    /// Closes every reader and drops every window. Idempotent, and it does not wait on a reader being
    /// warmed in the background — that warm disposes what it opened when it finds the workbook gone,
    /// so returning promptly still leaks no handle.
    /// <para>
    /// Disposing while a map is running is a caller error rather than corruption: the map fails with
    /// <see cref="ObjectDisposedException"/>, wrapped by the engine as a fault naming the shape and
    /// the cell it was reading.
    /// </para>
    /// </summary>
    public void Dispose()
    {
      lock (_gate)
      {
        if (_disposed)
          return;

        _disposed = true;

        foreach (var store in _stores.Values)
          store.Dispose();

        _parked?.Dispose();
        _parked = null;
      }

      _pool.Dispose();
    }

    /// <summary>One row of the catalogue: what the parked reader saw as it passed a sheet.</summary>
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
