using System.Collections.Generic;
using System.Linq;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What one workbook's readers have cost, shared by every sheet of it. A reader is a position in a
  /// <em>workbook</em>, not a sheet, so these counters belong to the book.
  /// <para>
  /// The vocabulary is deliberately narrow, and one word is deliberately absent. An earlier
  /// prototype reported <c>Rewinds</c>, documented as "backward reaches" but computed as
  /// <c>Reopens + SpareOpens</c>; on a run with 2,932 genuine backward reaches — every one of them
  /// served cheaply by a parked reader — it reported <c>1</c>. A counter that answers a different
  /// question from the one its name asks is worse than no counter, so it does not exist here.
  /// <see cref="CheapRewinds"/> counts the reaches; <see cref="Reopens"/> counts what they cost.
  /// </para>
  /// <para>
  /// The two numbers to act on: <see cref="Reopens"/> above zero means the declaration keeps more
  /// passes open at once than the workbook has readers — raise <c>MaxReaders</c>. Everything else is
  /// evidence about how the pool got there.
  /// </para>
  /// </summary>
  public readonly struct ReaderPoolStatistics
  {
    internal ReaderPoolStatistics(
      int maxReaders,
      int readersOpen,
      long opens,
      long reopens,
      long spareOpens,
      long warmHits,
      long warmWaitMilliseconds,
      long cheapRewinds,
      IReadOnlyList<long> rowsPerReader)
    {
      MaxReaders = maxReaders;
      ReadersOpen = readersOpen;
      Opens = opens;
      Reopens = reopens;
      SpareOpens = spareOpens;
      WarmHits = warmHits;
      WarmWaitMilliseconds = warmWaitMilliseconds;
      CheapRewinds = cheapRewinds;
      RowsPerReader = rowsPerReader;
    }

    /// <summary>The ceiling on readers held open at once — <c>WorkbookOptions.MaxReaders</c>.</summary>
    public int MaxReaders { get; }

    /// <summary>How many readers are open right now.</summary>
    public int ReadersOpen { get; }

    /// <summary>
    /// File opens of every kind: the total number of expensive events. On a workbook whose parked
    /// reader was adopted this is <c>1 + SpareOpens + Reopens</c>.
    /// </summary>
    public long Opens { get; }

    /// <summary>
    /// Live readers thrown away and opened again because every one of them stood ahead of a wanted
    /// row. The old fixed cost, and the number the pool exists to drive to zero: it stays at zero
    /// while the passes open at one time do not outnumber the readers.
    /// </summary>
    public long Reopens { get; }

    /// <summary>A spare slot opened for the first time, on demand or by a warmer.</summary>
    public long SpareOpens { get; }

    /// <summary>
    /// Spare opens a background warmer had already paid for by the time they were wanted. The
    /// difference between a warmed pool and a cold one is entirely here.
    /// </summary>
    public long WarmHits { get; }

    /// <summary>
    /// Time a reach spent blocked on a warmer that had started but not finished. Waiting is still
    /// the right move — two opens of one file finish no sooner than one, and the loser's work is
    /// thrown away — but this is what that wait cost.
    /// </summary>
    public long WarmWaitMilliseconds { get; }

    /// <summary>
    /// Backward reaches served by a reader parked behind the target: no open, no re-stream, just the
    /// rows between. It goes <em>up</em> when the pool is working.
    /// </summary>
    public long CheapRewinds { get; }

    /// <summary>Rows each reader has moved over, skipped and read alike.</summary>
    public IReadOnlyList<long> RowsPerReader { get; }

    /// <summary>The one-line form, for reading a run.</summary>
    public override string ToString() =>
      $"readers {ReadersOpen}/{MaxReaders} | opens {Opens} | reopens {Reopens} | " +
      $"spare opens {SpareOpens} (warm {WarmHits}, waited {WarmWaitMilliseconds:N0}ms) | " +
      $"cheap rewinds {CheapRewinds:N0} | per reader {Rendered}";

    private string Rendered =>
      RowsPerReader is null ? "-" : string.Join("/", RowsPerReader.Select(rows => rows.ToString("N0")));
  }
}
