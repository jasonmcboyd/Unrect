namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What reading one sheet through a window has cost.
  /// <para>
  /// The numbers to act on are <see cref="ChunkReloads"/> and <see cref="WindowOverruns"/>: the
  /// first says rows were read more than once, the second says why. A window must be at least as
  /// tall as the tallest extent open at one time — a walk down a sheet has an open extent of one
  /// chunk, but a
  /// <c>HorizontalFlow</c> or an <c>Overlay</c> over a band has an open extent of the whole band —
  /// and undersizing it is not a gentle degradation. Measured: a ten-chunk window over a seven-chunk
  /// band took 0.01s; a four-chunk window over a thirteen-chunk band took 29.5s. Three orders of
  /// magnitude, from one chunk of shortfall.
  /// </para>
  /// <para>
  /// <b>The floor these numbers do not include.</b> <see cref="ResidentBytes"/> counts
  /// <see cref="Unrect.Core.CellValue"/> structs only. Strings that <c>Text</c> cells point at are
  /// owned by the reader's shared-string table, are not counted here, and do not shrink with the
  /// window. On a text-heavy sheet that table can dominate: streaming removes the materialised grid,
  /// not the parser.
  /// </para>
  /// </summary>
  public readonly struct StreamingStatistics
  {
    internal StreamingStatistics(
      string sheetName,
      int chunkRows,
      int windowChunks,
      long chunkLoads,
      long chunkReloads,
      long evictions,
      long windowOverruns,
      long rowsMaterialised,
      long rowsSkipped,
      int residentChunks,
      int peakResidentChunks,
      long bytesPerChunk)
    {
      SheetName = sheetName;
      ChunkRows = chunkRows;
      WindowChunks = windowChunks;
      ChunkLoads = chunkLoads;
      ChunkReloads = chunkReloads;
      Evictions = evictions;
      WindowOverruns = windowOverruns;
      RowsMaterialised = rowsMaterialised;
      RowsSkipped = rowsSkipped;
      ResidentChunks = residentChunks;
      PeakResidentChunks = peakResidentChunks;
      ResidentBytes = residentChunks * bytesPerChunk;
      PeakResidentBytes = peakResidentChunks * bytesPerChunk;
    }

    /// <summary>The sheet these numbers describe.</summary>
    public string SheetName { get; }

    /// <summary>Rows in one chunk — the unit the window is loaded and evicted in.</summary>
    public int ChunkRows { get; }

    /// <summary>The window budget, in chunks.</summary>
    public int WindowChunks { get; }

    /// <summary>The window budget, in rows: <see cref="ChunkRows"/> × <see cref="WindowChunks"/>.</summary>
    public int WindowRows => ChunkRows * WindowChunks;

    /// <summary>Chunks materialised, re-materialisations included.</summary>
    public long ChunkLoads { get; }

    /// <summary>
    /// Loads of a chunk this store had already thrown away. Zero for a walk down a sheet; above zero
    /// means something reached back into rows the window had moved past.
    /// <para>
    /// This is the cost meter. <see cref="WindowOverruns"/> says a band did not fit; this says what
    /// not fitting cost.
    /// </para>
    /// </summary>
    public long ChunkReloads { get; }

    /// <summary>Chunks dropped to stay inside the budget.</summary>
    public long Evictions { get; }

    /// <summary>
    /// How many times a band did not fit the window: once for each distinct extent too tall to be
    /// held, plus each eviction forced from inside a band that was being swept. Above zero means the
    /// window is smaller than the declaration needs, and raising <c>WindowRows</c> is the fix.
    /// <para>
    /// The two counters divide the labour: this one says <em>why</em> — a band did not fit — and
    /// <see cref="ChunkReloads"/> says <em>what it cost</em>, in rows that had to be read again.
    /// Overruns without reloads is a band that did not fit but was never swept twice, which costs
    /// nothing; overruns with many reloads is the collapse the sizing law exists to prevent.
    /// </para>
    /// </summary>
    public long WindowOverruns { get; }

    /// <summary>Rows read from the source and adapted into cells.</summary>
    public long RowsMaterialised { get; }

    /// <summary>
    /// Rows parsed and discarded to move a reader to a wanted chunk. Window sizing owns this
    /// number; the reader pool does not touch it, which is why it is invariant under
    /// <c>MaxReaders</c>.
    /// </summary>
    public long RowsSkipped { get; }

    /// <summary>Chunks held right now.</summary>
    public int ResidentChunks { get; }

    /// <summary>The most chunks ever held at once. Never exceeds <see cref="WindowChunks"/>.</summary>
    public int PeakResidentChunks { get; }

    /// <summary>Bytes of cells resident right now.</summary>
    public long ResidentBytes { get; }

    /// <summary>Bytes of cells resident at the peak.</summary>
    public long PeakResidentBytes { get; }

    /// <summary>The one-line form, for reading a run.</summary>
    public override string ToString() =>
      $"'{SheetName}' chunk {ChunkRows}r x {WindowChunks} ({WindowRows:N0} rows) | " +
      $"loads {ChunkLoads:N0} (reloads {ChunkReloads:N0}) | evictions {Evictions:N0} | " +
      $"overruns {WindowOverruns:N0} | rows read {RowsMaterialised:N0} skipped {RowsSkipped:N0} | " +
      $"resident {ResidentChunks} chunks / {ResidentBytes:N0}B (peak {PeakResidentChunks} / {PeakResidentBytes:N0}B)";
  }
}
