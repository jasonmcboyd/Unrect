using System;
using System.Threading;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// One sheet, held a window at a time: rows in fixed-size chunks, a budget on how many may be
  /// resident, and a reader pool to materialise the rest on demand.
  ///
  /// <para><b>The sizing law.</b> The window must be at least as tall as the tallest extent open at
  /// one time. A walk down a sheet holds one chunk open; a <c>HorizontalFlow</c> or <c>Overlay</c>
  /// over a band holds the whole band. Undersize it and the cost is not degradation but collapse —
  /// a ten-chunk window over a seven-chunk band measured 0.01s, a four-chunk window over a
  /// thirteen-chunk band 29.5s. <see cref="StreamingStatistics.ChunkReloads"/> and
  /// <see cref="StreamingStatistics.WindowOverruns"/> are what report it.</para>
  ///
  /// <para><b>The residency law.</b> A chunk overlapping the band currently being swept — the
  /// <em>locus</em> — is not an eviction candidate; least-recently-used is the tie-break outside it,
  /// and only ever the tie-break. Plain LRU is precisely wrong for a repeated sweep of a band one
  /// chunk larger than the budget, where the least-recently-used chunk is always the one wanted
  /// next.</para>
  ///
  /// <para><b>Concurrency.</b> Reads are lock-free on the resident path. A stale miss costs one trip
  /// through the gate, which re-checks; a stale hit is a chunk another thread evicted, whose
  /// contents are immutable and still correct. Recency bookkeeping is deliberately unsynchronised —
  /// a lost increment picks a slightly worse victim and nothing else. Loads serialise on this
  /// store's gate, so parallel maps over <em>one</em> sheet are serial; different sheets of one
  /// workbook are not, which is why the lock order is store gate then pool gate, never the reverse.
  /// </para>
  /// </summary>
  internal sealed class SheetStore : IDisposable
  {
    /// <summary>
    /// The size of one <see cref="CellValue"/>, and the reason chunks are sized the way they are.
    /// <para>
    /// It was 8 while <c>CellValue</c> was a class holding a reference. Leaving it at 8 after the
    /// struct merge would have tripled every chunk — a default 8-column chunk would have been
    /// 196,608 bytes, straight onto the Large Object Heap that the 64 KB target exists to avoid.
    /// A test asserts this against <c>Unsafe.SizeOf&lt;CellValue&gt;()</c> so the next
    /// representation change cannot repeat it.
    /// </para>
    /// </summary>
    internal const int BytesPerCell = 24;

    /// <summary>
    /// Chunks are sized to stay under the 85,000-byte Large Object Heap threshold, because they are
    /// allocated and dropped continuously as the window slides, and an LOH allocation per chunk
    /// would trade a bounded heap for a fragmenting one.
    /// </summary>
    internal const int TargetChunkBytes = 65536;

    /// <summary>The smallest window worth having: below four chunks, eviction thrashes on its own.</summary>
    internal const int MinimumWindowChunks = 4;

    private readonly object _gate = new object();
    private readonly ReaderPool _pool;
    private readonly int _sheetIndex;
    private readonly CellValue[]?[] _resident;
    private readonly long[] _recency;

    private long _tick;
    private int _locusFrom;
    private int _locusTo;
    private int _oversizedFrom = -1;
    private int _oversizedTo = -1;
    private int _liveResident;
    private int _peakResident;
    private bool _disposed;

    private long _chunkLoads;
    private long _chunkReloads;
    private long _evictions;
    private long _windowOverruns;
    private long _rowsMaterialised;
    private long _rowsSkipped;

    internal SheetStore(
      ReaderPool pool,
      int sheetIndex,
      string sheetName,
      int rowCount,
      int columnCount,
      int chunkRows,
      int windowChunks)
    {
      if (rowCount <= 0)
        // Some xlsx files carry no dimension element, and the resident index is sized from the row
        // count. Failing loudly beats silently truncating a sheet to nothing.
        throw new NotSupportedException(
          $"The sheet '{sheetName}' does not report a row count, which the streaming reader needs to " +
          "index it. Read this workbook with SpreadsheetSpace.Create instead.");

      _pool = pool;
      _sheetIndex = sheetIndex;
      SheetName = sheetName;
      RowCount = rowCount;

      // Not clamped to 1: a sheet with no columns is 0 wide, which is exactly what the eager path
      // reports for the same file. Widening it to 1 would hand back a column of blanks that the
      // other path does not have, and identity between the two paths is worth more than avoiding a
      // degenerate case — a 0-wide store simply holds nothing.
      ColumnCount = Math.Max(0, columnCount);
      ChunkRows = chunkRows > 0 ? chunkRows : DefaultChunkRows(ColumnCount);
      WindowChunks = Math.Max(MinimumWindowChunks, windowChunks);

      var chunks = (RowCount + ChunkRows - 1) / ChunkRows;
      _resident = new CellValue[]?[chunks];
      _recency = new long[chunks];
    }

    internal string SheetName { get; }

    internal int RowCount { get; }

    internal int ColumnCount { get; }

    internal int ChunkRows { get; }

    internal int WindowChunks { get; }

    /// <summary>
    /// Rows per chunk for a sheet this wide: as many as fit the 64 KB target, clamped so a very wide
    /// sheet still gets a whole row and a very narrow one does not get an absurd band.
    /// <para>
    /// The Large Object Heap guarantee holds up to about 3,541 columns (85,000 / 24). Beyond that a
    /// single row exceeds the threshold on its own and a chunk cannot hold less than a row, so a
    /// very wide sheet allocates on the LOH whatever this returns. Nothing can be done about that
    /// here; it is a property of the sheet.
    /// </para>
    /// </summary>
    internal static int DefaultChunkRows(int columnCount)
    {
      if (columnCount <= 0)
        return 1;

      return Math.Max(1, Math.Min(8192, TargetChunkBytes / (BytesPerCell * columnCount)));
    }

    /// <summary>
    /// Rows of window budget converted to whole chunks, never fewer than the minimum.
    /// <para>
    /// The division is done on <see cref="long"/> because the rounding-up idiom overflows for a
    /// window within <c>chunkRows</c> of <see cref="int.MaxValue"/> — which would silently produce a
    /// negative chunk count and a window that holds nothing.
    /// </para>
    /// </summary>
    internal static int WindowChunksFor(int windowRows, int chunkRows)
    {
      var chunks = ((long)windowRows + chunkRows - 1) / chunkRows;

      return (int)Math.Max(MinimumWindowChunks, Math.Min(int.MaxValue, chunks));
    }

    /// <summary>
    /// One cell, loading its chunk if the window no longer holds it.
    /// <para>
    /// <paramref name="extentTop"/> and <paramref name="extentHeight"/> are the locus signal. They
    /// are the one thing the view knows and this store does not: whether the read belongs to a
    /// bounded sweep of a band, or a walk down the sheet.
    /// </para>
    /// </summary>
    internal CellValue GetCell(int column, int row, int extentTop, int extentHeight)
    {
      // Before everything, including the resident fast path: a read after the workbook is disposed
      // must fail whether or not the chunk happens still to be in memory.
      if (Volatile.Read(ref _disposed))
        throw new ObjectDisposedException("Workbook", $"The workbook owning sheet '{SheetName}' has been disposed.");

      var chunk = row / ChunkRows;
      var cells = Volatile.Read(ref _resident[chunk]);

      if (cells is null)
      {
        cells = Load(chunk, extentTop, extentHeight);
      }
      else
      {
        Touch(chunk);

        // The locus is recorded on every resident read, not only when the chunk changes. Gating it
        // on a chunk transition read an unsynchronised field to decide whether to apply the
        // residency law at all — so under two threads the law was applied or skipped by a race,
        // and a band could lose its anchor precisely when it was being swept. Recency bookkeeping
        // stays deliberately unsynchronised because a lost increment picks a slightly worse victim;
        // dropping an anchor is not that harmless.
        lock (_gate)
          Anchor(extentTop, extentHeight);
      }

      return cells[((row - (chunk * ChunkRows)) * ColumnCount) + column];
    }

    private CellValue[] Load(int chunk, int extentTop, int extentHeight)
    {
      lock (_gate)
      {
        Anchor(extentTop, extentHeight);

        // Another thread may have loaded it between the plain read above and this gate.
        var existing = _resident[chunk];
        if (existing is not null)
        {
          Touch(chunk);
          return existing;
        }

        var start = chunk * ChunkRows;
        var rows = Math.Min(ChunkRows, RowCount - start);

        // No pre-fill: default(CellValue) IS Blank, so a freshly allocated chunk is already an
        // all-blank band and a short row leaves the cells it never reached exactly right.
        var cells = new CellValue[rows * ColumnCount];

        var lease = _pool.Borrow(_sheetIndex, start, out var skipped);
        _rowsSkipped += skipped;

        try
        {
          var cursor = lease.Cursor!;

          for (var r = 0; r < rows && cursor.Read(); r++)
          {
            lease.CountRow();
            _rowsMaterialised++;

            for (var c = 0; c < ColumnCount; c++)
              cells[(r * ColumnCount) + c] = cursor[c];
          }
        }
        finally
        {
          _pool.Return(lease);
        }

        Evict();

        _chunkLoads++;
        if (_recency[chunk] != 0)
          _chunkReloads++;

        Volatile.Write(ref _resident[chunk], cells);
        _liveResident++;
        if (_liveResident > _peakResident)
          _peakResident = _liveResident;

        Touch(chunk);

        return cells;
      }
    }

    /// <summary>
    /// Drops chunks until there is room for one more.
    /// <para>
    /// A chunk inside the locus is not a candidate while anything is still sweeping that band;
    /// least-recently-used is the tie-break outside it. When no candidate exists outside the locus,
    /// something inside it is evicted anyway and a
    /// <see cref="StreamingStatistics.WindowOverruns"/> is recorded — as it is when a band arrives
    /// too tall to be anchored at all.
    /// </para>
    /// </summary>
    private void Evict()
    {
      while (_liveResident >= WindowChunks)
      {
        var victim = Victim(respectLocus: true);

        if (victim < 0)
        {
          victim = Victim(respectLocus: false);

          if (victim >= 0)
            _windowOverruns++;
        }

        if (victim < 0)
          return;

        Volatile.Write(ref _resident[victim], null);
        _liveResident--;
        _evictions++;
      }
    }

    private int Victim(bool respectLocus)
    {
      var victim = -1;
      var oldest = long.MaxValue;

      for (var i = 0; i < _resident.Length; i++)
      {
        if (_resident[i] is null)
          continue;

        if (respectLocus && InLocus(i))
          continue;

        if (_recency[i] < oldest)
        {
          oldest = _recency[i];
          victim = i;
        }
      }

      return victim;
    }

    /// <summary>
    /// Records the band a read belongs to. The locus grows by union, so nested single-row extents
    /// inside a band do not shrink it to a row, and re-anchors when the union would exceed the
    /// budget — an extent too big to hold cannot be pinned, and anchoring on it would pin the wrong
    /// thing.
    /// <para>
    /// An extent that does not fit the window at all is the sizing law being broken, and counts a
    /// <see cref="StreamingStatistics.WindowOverruns"/>: the window is smaller than the band the
    /// declaration is sweeping. Counted once per band rather than once per cell.
    /// </para>
    /// </summary>
    private void Anchor(int extentTop, int extentHeight)
    {
      if (extentHeight <= 0)
        return;

      var from = extentTop;
      var to = extentTop + extentHeight;
      var budget = WindowChunks * ChunkRows;

      if (_locusTo > _locusFrom)
      {
        var unionFrom = Math.Min(_locusFrom, from);
        var unionTo = Math.Max(_locusTo, to);

        if (unionTo - unionFrom <= budget)
        {
          _locusFrom = unionFrom;
          _locusTo = unionTo;
          return;
        }
      }

      if (to - from <= budget)
      {
        _locusFrom = from;
        _locusTo = to;
      }
      else
      {
        // The band cannot be held at all, so there is nothing to anchor: this is the sizing law
        // being broken, and it is the event WindowOverruns counts. Deduplicated on the extent,
        // because the view hands the same extent down with every cell of it — one band that did
        // not fit is one overrun, not one per read.
        if (from != _oversizedFrom || to != _oversizedTo)
        {
          _oversizedFrom = from;
          _oversizedTo = to;
          _windowOverruns++;
        }

        _locusFrom = 0;
        _locusTo = 0;
      }
    }

    private bool InLocus(int chunk)
    {
      if (_locusTo <= _locusFrom)
        return false;

      var start = chunk * ChunkRows;

      return start < _locusTo && start + ChunkRows > _locusFrom;
    }

    private void Touch(int chunk)
    {
      _recency[chunk] = ++_tick;
    }

    /// <summary>A snapshot of what reading this sheet has cost, taken under the gate.</summary>
    internal StreamingStatistics Snapshot()
    {
      lock (_gate)
        return new StreamingStatistics(
          SheetName,
          ChunkRows,
          WindowChunks,
          _chunkLoads,
          _chunkReloads,
          _evictions,
          _windowOverruns,
          _rowsMaterialised,
          _rowsSkipped,
          _liveResident,
          _peakResident,
          (long)ChunkRows * ColumnCount * BytesPerCell);
    }

    /// <summary>Drops every resident chunk. The readers belong to the workbook, not to this store.</summary>
    public void Dispose()
    {
      Volatile.Write(ref _disposed, true);

      lock (_gate)
      {
        for (var i = 0; i < _resident.Length; i++)
          Volatile.Write(ref _resident[i], null);

        _liveResident = 0;
      }
    }
  }
}
