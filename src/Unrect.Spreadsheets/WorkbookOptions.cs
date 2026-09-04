using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// How a <see cref="Workbook"/> reads: what counts as empty, how much of a sheet it holds at once,
  /// and how many readers it keeps open.
  /// <para>
  /// A class with <c>init</c> accessors rather than a parameter list, so a future option is an
  /// additive member that breaks no existing call site — the same discipline the rest of the library
  /// follows for anything that will grow.
  /// </para>
  /// </summary>
  public sealed class WorkbookOptions
  {
    /// <summary>
    /// Which cells count as empty space. The default treats whitespace-only text as blank, exactly
    /// as <see cref="SpreadsheetSpace.Create(string, string, bool, Func{CellValue, bool})"/> does:
    /// exported workbooks are full of <c>"  "</c> cells that look empty, are meant to be empty, and
    /// would otherwise anchor a region. Pass <c>_ =&gt; false</c> for strict fidelity.
    /// </summary>
    public Func<CellValue, bool>? IsBlank { get; init; }

    /// <summary>
    /// The memory knob, in rows. It must be at least as tall as the tallest extent open at one time:
    /// a walk down a sheet holds one chunk, but a <c>HorizontalFlow</c> or <c>Overlay</c> over a band
    /// holds the whole band, and a window one chunk short of the band it sweeps is the difference
    /// between 0.01s and 29.5s. Rounded up to whole chunks, with a floor of four.
    /// </summary>
    public int WindowRows { get; init; } = 8192;

    /// <summary>
    /// Rows per chunk, or 0 to derive one from the sheet's width — as many rows as fit 64 KB, which
    /// keeps a chunk off the Large Object Heap it would otherwise fragment.
    /// <para>
    /// Setting it large is a footgun and the default exists to avoid it: the window is
    /// <em>four chunks at minimum</em>, so a chunk of 100,000 rows means a floor of 400,000 rows
    /// resident however small <see cref="WindowRows"/> is, and every chunk allocated goes to the
    /// Large Object Heap that continuous allocation and release will fragment. Raise
    /// <see cref="WindowRows"/> to hold more of a sheet; leave this alone unless a measurement says
    /// otherwise.
    /// </para>
    /// </summary>
    public int ChunkRows { get; init; }

    /// <summary>
    /// The most readers this workbook holds open at once.
    /// <para>
    /// Three by default, and deliberately not generous: two readers remove every reopen for the
    /// ordinary bound-then-project pattern, a third helps when several passes are open at once, and
    /// each additional reader costs both a multi-second open and another resident copy of the
    /// workbook's shared-string table. <see cref="ReaderPoolStatistics.Reopens"/> above zero is the
    /// evidence that a particular declaration wants more.
    /// </para>
    /// </summary>
    public int MaxReaders { get; init; } = 3;

    /// <summary>
    /// Whether spare readers are opened ahead of need on a background task. On by default, because
    /// warming is where the pool's value actually is: measured, two unwarmed readers cost the same
    /// as one, and the same two warmed cut a 24.6s parse to 19.6s. Off is for tests and measurement,
    /// and costs about one open per pass.
    /// </summary>
    public bool WarmReaders { get; init; } = true;

    /// <summary>
    /// Whether sheet names must match exactly. Off by default, as the eager path.
    /// </summary>
    public bool CaseSensitiveSheetNames { get; init; }

    /// <summary>
    /// The most distinct text values this workbook will keep so that equal <c>Text</c> cells can
    /// share one instance of their characters instead of holding a copy each. It is the other memory
    /// knob: <see cref="WindowRows"/> bounds the cells held, and this bounds what their <em>strings</em>
    /// cost — see <see cref="Workbook.InterningStatistics"/> for what it earns.
    /// <para>
    /// 65,536 by default, which is generous on purpose. What actually repeats in a workbook —
    /// captions, currency and account codes, categories, party names — runs to hundreds or a few
    /// thousand distinct values per sheet and tens of thousands across a large multi-sheet book, so
    /// the default holds every one of them for nearly every real file while bounding what the table
    /// itself can pin. Only strings of 256 characters or fewer are ever entered (a memo field is
    /// almost never repeated and would occupy an entry that never scores a hit), so this is a real
    /// ceiling: roughly this many times 530 bytes of characters, plus the table's own ~56 bytes an
    /// entry — some 40 MB at the default were every entry a full 256 characters, and a small fraction
    /// of that in practice.
    /// </para>
    /// <para>
    /// Past the cap the table stops growing rather than failing: values already in it go on being
    /// shared, and one first met afterwards keeps the instance it arrived with.
    /// <see cref="InterningStatistics.AtCapacity"/> is what reports having got there — and it says
    /// nothing on its own about which way to move this. Reaching the cap costs nothing
    /// <em>in sharing</em>: a column that never repeats (a transaction reference, an invoice number)
    /// fills any cap without displacing anything, because the values that <em>do</em> repeat are met
    /// in the sheet's first rows and are in the table long before it fills. What it costs is the
    /// entries — held for the life of the workbook whether they ever score a hit or not, and beside a
    /// window of single-digit MB that is the larger number. So raise this when a sheet's genuinely
    /// repeating vocabulary is larger than the cap, and lower it when the text does not repeat and
    /// the memory floor is the point. Zero turns sharing off entirely, for measurement or for a
    /// workbook whose text is known to be unique.
    /// </para>
    /// <para>
    /// How much a wasted entry actually costs depends on the file. Where an <c>.xlsx</c> spells its
    /// text through the workbook's shared-string table, the reader pins those strings for its own
    /// lifetime anyway and an entry here adds its dictionary node rather than its characters. The
    /// cost lands on files whose text is inline, and on <c>.xls</c>.
    /// </para>
    /// </summary>
    public int MaxInternedStrings { get; init; } = DefaultMaxInternedStrings;

    /// <inheritdoc cref="MaxInternedStrings"/>
    internal const int DefaultMaxInternedStrings = 65_536;

    /// <summary>
    /// The largest chunk this accepts. Four of them is a 4,000,000-row window floor, which is
    /// already far past any sensible budget.
    /// </summary>
    internal const int MaximumChunkRows = 1_000_000;

    internal void Validate()
    {
      if (WindowRows < 1)
        throw new ArgumentOutOfRangeException(nameof(WindowRows), WindowRows, "The window must be at least one row.");

      if (MaxReaders < 1)
        throw new ArgumentOutOfRangeException(nameof(MaxReaders), MaxReaders, "A workbook needs at least one reader.");

      if (MaxInternedStrings < 0)
        throw new ArgumentOutOfRangeException(
          nameof(MaxInternedStrings),
          MaxInternedStrings,
          "The interning cap cannot be negative; pass 0 to share nothing.");

      if (ChunkRows < 0)
        throw new ArgumentOutOfRangeException(nameof(ChunkRows), ChunkRows, "Rows per chunk cannot be negative; pass 0 to derive it from the sheet's width.");

      // An upper bound as well as a lower one: the minimum window is four chunks, so an enormous
      // chunk silently overrides the memory knob it is supposed to serve.
      if (ChunkRows > MaximumChunkRows)
        throw new ArgumentOutOfRangeException(
          nameof(ChunkRows),
          ChunkRows,
          $"Rows per chunk is capped at {MaximumChunkRows}: the window holds at least four chunks, so a larger one would set a resident floor of its own regardless of WindowRows.");
    }
  }
}
