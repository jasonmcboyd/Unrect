using System;
using System.Collections.Concurrent;
using System.Threading;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The find-my-twin table for the streaming door: one canonical instance per distinct string, so
  /// equal <c>Text</c> cells point at the same characters instead of each holding a copy of them.
  ///
  /// <para><b>It saves retention, not allocation.</b> The duplicate has already been built by the
  /// reader before this ever sees it — that is the reader's business and there is no reaching into
  /// it. What changes is what survives the fill: the twin dies in gen0 and the cells share the one
  /// instance the table kept. A profiler's <c>Allocated</c> column will not move; the live set of a
  /// held grid or a held projection will.</para>
  ///
  /// <para><b>Strings only.</b> Every other kind is inline in the 24-byte
  /// <see cref="CellValue"/> — a number, a date, a boolean and a blank hold no heap object to
  /// share — and the two that could (a number's exact decimal, an error's literal) are unreachable
  /// through the spreadsheet door: a reader hands the adapter a <c>double</c> for every numeric cell,
  /// and an error's literal is kept only when it is one the canonical spelling does not already
  /// name.</para>
  ///
  /// <para><b>One table per workbook.</b> Captions, codes and categories repeat across the sheets of
  /// one book, and a chase reader re-parsing rows the window has dropped must find the values the
  /// first parse already canonicalised rather than start a second family of them. Concurrent for the
  /// same reason: chunk fills for different sheets of one workbook run on the borrowing threads,
  /// unserialised by any single sheet's gate.</para>
  ///
  /// <para><b>The two guards, and why a long-lived table needs them.</b> A table that outlives the
  /// window it fed can pin strings the window has long since evicted, so it must never grow without
  /// bound and never keep what is not earning its keep:</para>
  /// <list type="bullet">
  ///   <item><see cref="Capacity"/> — past it the table stops <em>growing</em>. Lookups still hit, so
  ///     everything already shared goes on being shared; a value first seen afterwards is handed back
  ///     unchanged and simply does not dedup. Degradation, never failure.</item>
  ///   <item><see cref="MaximumLength"/> — a string longer than this is never entered. Long text in a
  ///     spreadsheet is a memo or a free-text note: nearly always unique, so it would occupy an entry
  ///     that never scores a hit while pinning the most bytes of anything in the table, and it costs
  ///     the most to hash and compare on every cell that goes past. What actually repeats — a
  ///     caption, a currency code, a category, a party name — is short.</item>
  /// </list>
  /// </summary>
  internal sealed class StringInterner
  {
    /// <summary>
    /// The longest string worth sharing, in characters. 256 sits well above every label, code and
    /// category a sheet repeats, and well below the memo fields that would otherwise fill the table
    /// with entries that never hit. It also bounds the table's own retention with
    /// <see cref="Capacity"/>: the two together cap what a workbook can pin here at roughly
    /// <c>Capacity × 530</c> bytes.
    /// <para>
    /// The eager door applies the same guard (see <c>SpreadsheetSpace</c>), so the two doors share
    /// the same values as each other and differ in nothing a caller can observe.
    /// </para>
    /// </summary>
    internal const int MaximumLength = 256;

    /// <summary>
    /// Bytes of object header and method-table pointer on a 64-bit runtime — the fixed part of what a
    /// <see cref="string"/> costs, and the reason a byte figure computed from lengths alone would
    /// understate what sharing saved.
    /// </summary>
    private const int ObjectHeaderBytes = 16;

    private readonly ConcurrentDictionary<string, string> _entries =
      new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Entries held, counted here rather than read off the dictionary: <c>ConcurrentDictionary.Count</c>
    /// takes every bucket lock, and the cap is tested once per text cell.
    /// </summary>
    private int _count;

    private long _hits;
    private long _bytesSaved;

    internal StringInterner(int capacity)
    {
      Capacity = capacity;
    }

    /// <summary>
    /// The most distinct values this table will hold. Zero turns sharing off: nothing is ever
    /// entered, so every lookup misses and every cell keeps the instance it arrived with.
    /// </summary>
    internal int Capacity { get; }

    /// <summary>
    /// <paramref name="value"/> with its text replaced by the canonical instance, or unchanged when
    /// there is nothing to share — every kind but <c>Text</c>, a <c>Text</c> this table has not seen
    /// before, and a <c>Text</c> either guard declined. The returned cell is equal to the one passed
    /// in by every measure a caller has; only its identity differs.
    /// </summary>
    internal CellValue Share(CellValue value)
    {
      if (value.TryGetString() is not string text || text.Length > MaximumLength)
        return value;

      if (_entries.TryGetValue(text, out var canonical))
      {
        Interlocked.Increment(ref _hits);
        Interlocked.Add(ref _bytesSaved, Bytes(text.Length));

        return CellValue.Of(canonical);
      }

      // Two threads entering the same new value is the one race here, and it is harmless: the loser
      // adds nothing, that cell alone goes unshared, and the next read of the same value hits the
      // winner's entry. Counting slightly past the cap under the same race costs an entry, not
      // correctness.
      if (Volatile.Read(ref _count) < Capacity && _entries.TryAdd(text, text))
        Interlocked.Increment(ref _count);

      return value;
    }

    /// <summary>
    /// Drops every entry, keeping the counters — see <see cref="Snapshot"/>.
    /// <para>
    /// <b>Once, at the table's end of life.</b> A <see cref="Share"/> that followed or raced this
    /// would repopulate the entries it just dropped and count again values already counted: the
    /// entry count is the count <em>reached</em>, so it is not reset here and cannot be without
    /// costing <see cref="InterningStatistics.DistinctValues"/> its meaning after a workbook is
    /// disposed. Nothing in the type enforces that; the workbook's disposal order is what makes it
    /// unreachable, because a chunk fill holds its store's gate and every store is disposed — which
    /// waits on that gate — before this is called.
    /// </para>
    /// </summary>
    internal void Release() => _entries.Clear();

    /// <summary>
    /// What sharing has cost and earned. Readable after the table has been
    /// <see cref="Release"/>d, because the counters describe reading that has already happened:
    /// <see cref="InterningStatistics.DistinctValues"/> is the count the table reached, not the
    /// number of entries alive at the moment of asking.
    /// </summary>
    internal InterningStatistics Snapshot() =>
      new InterningStatistics(
        Volatile.Read(ref _count),
        Capacity,
        Interlocked.Read(ref _hits),
        Interlocked.Read(ref _bytesSaved));

    /// <summary>
    /// What one string of this length occupies: the header, the length field, and the characters with
    /// their terminator, rounded to the runtime's eight-byte allocation granularity. An estimate of
    /// the layout rather than a measurement of the heap, which is why every figure computed from it
    /// says so in its name.
    /// </summary>
    private static long Bytes(int length) =>
      (ObjectHeaderBytes + sizeof(int) + (2L * (length + 1)) + 7) / 8 * 8;
  }
}
