namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What sharing repeated text has earned one <see cref="Workbook"/>. Equal <c>Text</c> cells are
  /// given one instance of their characters rather than one each, so a held grid or a held projection
  /// keeps the distinct values of a sheet instead of a copy per cell.
  /// <para>
  /// <b>These are retention numbers, not allocation numbers.</b> The duplicate is built by the reader
  /// before the adapter sees it; what sharing removes is the copy that would have <em>survived</em>,
  /// not the one that was made. A profiler's allocation column will not move.
  /// </para>
  /// <para>
  /// <b>They belong to the book, not to a sheet.</b> One table serves every sheet of a workbook,
  /// because captions and codes repeat across sheets and a reader re-parsing rows the window dropped
  /// must find the values the first parse canonicalised. Splitting these counters per sheet would
  /// invite adding up numbers that do not add up.
  /// </para>
  /// <para>
  /// <see cref="AtCapacity"/> is the one thing here a caller acts on, and it wants reading with care.
  /// While it is false the table shared everything it was offered; while it is true, a value first
  /// seen after the cap was reached goes unshared. Reaching it costs nothing <em>in sharing</em>: a
  /// column that never repeats — a transaction reference, an invoice number — fills any cap without
  /// displacing anything, because the values that <em>do</em> repeat are met in a sheet's first rows
  /// and are in the table long before it fills.
  /// </para>
  /// <para>
  /// What it costs is the entries themselves. Every one is held for the life of the workbook whether
  /// it ever scores a hit or not, at roughly <c>Capacity × 530</c> bytes of characters plus the
  /// table's own ~56 bytes an entry — at the default cap of 65,536, some 40 MB were every entry a
  /// 256-character string, against a default window of about 1.5 MB on an eight-column sheet. So
  /// raise <c>WorkbookOptions.MaxInternedStrings</c> when a sheet's genuinely repeating vocabulary is
  /// larger than the cap, and <em>lower</em> it — or pass 0 — when the text does not repeat and the
  /// floor is the point.
  /// </para>
  /// <para>
  /// How much a wasted entry actually costs depends on the file. Where an <c>.xlsx</c> spells its text
  /// through the workbook's shared-string table, the reader holds those strings for its own lifetime
  /// anyway and an entry here adds its dictionary node rather than its characters — the marginal cost
  /// is near nothing. The cost lands where the text is inline, and on <c>.xls</c>.
  /// </para>
  /// </summary>
  public readonly struct InterningStatistics
  {
    internal InterningStatistics(int distinctValues, int capacity, long hits, long estimatedBytesSaved)
    {
      DistinctValues = distinctValues;
      Capacity = capacity;
      Hits = hits;
      EstimatedBytesSaved = estimatedBytesSaved;
    }

    /// <summary>
    /// Distinct values the table took in. Never above <see cref="Capacity"/> by more than the
    /// handful two threads can add at once, and it does not fall when the table is dropped at
    /// <c>Dispose</c> — it is the count reached, not the entries alive at the moment of asking.
    /// </summary>
    public int DistinctValues { get; }

    /// <summary>The ceiling on <see cref="DistinctValues"/> — <c>WorkbookOptions.MaxInternedStrings</c>.</summary>
    public int Capacity { get; }

    /// <summary>
    /// Whether the table stopped growing. Past the cap everything already in it goes on being shared
    /// and a newly met value does not — degradation, never failure. True for a
    /// <see cref="Capacity"/> of zero, which is sharing turned off and is the one form of "at
    /// capacity" a caller asked for. Neither a reason to raise the cap by itself nor a reason to
    /// leave it alone — see this type's summary for what the entries cost while they sit there.
    /// </summary>
    public bool AtCapacity => DistinctValues >= Capacity;

    /// <summary>
    /// Cells handed an instance the table already held. Each one is a duplicate string that did not
    /// have to be retained. Counted per fill rather than per cell: a chunk reloaded after eviction
    /// shares its cells again and counts them again, so this can exceed a sheet's text-cell count —
    /// read it against <c>StreamingStatistics.ChunkReloads</c>.
    /// </summary>
    public long Hits { get; }

    /// <summary>
    /// What those hits are worth, in bytes — <b>estimated</b>, and named so. It is the sum over every
    /// hit of what a string of that length occupies on a 64-bit runtime (header, length, characters,
    /// rounded to the allocation granularity), which is what the duplicate <em>would</em> have cost
    /// had it been kept, and it counts a reloaded chunk's cells again exactly as <see cref="Hits"/>
    /// does.
    /// It models the layout, not the heap: it does not know what the collector would have done with
    /// the bytes, and it is not a measurement of a live set. For that, measure the live set.
    /// </summary>
    public long EstimatedBytesSaved { get; }

    /// <summary>
    /// The one-line form, for reading a run. The capacity clause appears only when the table actually
    /// filled, on the same footing as every other conditional clause in this vocabulary: it is the
    /// one thing here a caller would act on, and a run that never reached the cap has nothing to say.
    /// </summary>
    public override string ToString() =>
      $"shared {Hits:N0} | distinct {DistinctValues:N0}/{Capacity:N0} | " +
      $"saved ~{EstimatedBytesSaved:N0}B (estimated)" +
      (AtCapacity ? " | AT CAPACITY" : string.Empty);
  }
}
