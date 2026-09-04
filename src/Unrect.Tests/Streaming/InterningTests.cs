using System;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The find-my-twin table: equal <c>Text</c> cells given one instance of their characters, and the
  /// two guards that stop a long-lived table pinning more than it earns.
  /// <para>
  /// It lives beside the streaming tests because the table does — <c>StringInterner</c> is the
  /// workbook's, scoped to the book and plumbed through every sheet store — but the law it states is
  /// not streaming's alone: the eager door keeps a table of its own to the same rules, and the half
  /// of these pins that a file can carry is in <c>SpreadsheetSpaceTests</c>, where that door's tests
  /// are.
  /// </para>
  /// <para>
  /// <b>Nothing here reads <c>StreamingStatistics</c>, and that is a design fact rather than an
  /// oversight.</b> The counters belong to the book, not to a sheet — one table serves them all, so a
  /// per-sheet split would report numbers that do not add up — which is why the sheet's one-line form
  /// is byte-for-byte what it was before sharing existed. The test below says so out loud so the next
  /// person to want a per-sheet figure finds the reason before the change.
  /// </para>
  /// </summary>
  public class InterningTests
  {
    /// <summary>
    /// A text cell holding a FRESH instance of <paramref name="value"/> — what a reader spelling its
    /// strings inline hands the adapter, and the only arrangement under which sharing is observable
    /// at all. A literal would be interned by the runtime and every assertion here would pass for
    /// the wrong reason.
    /// </summary>
    private static CellValue Fresh(string value) => CellValue.Of(new string(value.ToCharArray()));

    /// <summary>A sheet whose every cell is a fresh copy of one repeated value.</summary>
    private static FakeSheet Repeating(string name, int rows, int columns, string value = "Capital Call") =>
      new FakeSheet(name, rows, columns, (_, _) => Fresh(value));

    private static SheetStore Store(FakeRowSource source, int rows, int columns, int chunkRows, int windowChunks) =>
      new SheetStore(new ReaderPool(source, 2, warmReaders: false), 0, "Data", rows, columns, chunkRows, windowChunks);

    /// <summary>A workbook over <paramref name="source"/>, warming off so the counts are deterministic.</summary>
    private static Workbook Book(FakeRowSource source, WorkbookOptions? options = null) =>
      Workbook.Over(source, options ?? new WorkbookOptions { WarmReaders = false });

    // --- The table -------------------------------------------------------------------------------

    [Fact]
    public void TheFirstSightingOfAValueKeepsItsOwnInstanceAndEveryLaterOneJoinsIt()
    {
      // The whole mechanism in one test. The table does not copy: it keeps the instance the first
      // cell arrived with and hands that one to every equal cell after it, so what is removed is the
      // duplicate's SURVIVAL, not its allocation — the reader had already built it before this saw it.
      var table = new StringInterner(64);
      var first = Fresh("Alpha Fund");
      var second = Fresh("Alpha Fund");

      Assert.NotSame(first.GetString(), second.GetString());   // the reader's two copies, as they arrive

      var kept = table.Share(first);
      var joined = table.Share(second);

      Assert.Same(first.GetString(), kept.GetString());
      Assert.Same(kept.GetString(), joined.GetString());

      // Equal by every measure a caller has; only the identity differs, which is the promise that
      // makes sharing invisible.
      Assert.Equal(first, joined);
      Assert.Equal("Alpha Fund", joined.GetString());
    }

    [Fact]
    public void AllUniqueTextSharesNothingAndSaysSo()
    {
      // The control. A column that never repeats — a transaction reference, an invoice number — is
      // the case the table cannot help, and it must say that rather than appear to have helped: no
      // hits, no bytes, and every cell still holding the instance it arrived with.
      var table = new StringInterner(1024);
      var cells = new CellValue[20];

      for (var index = 0; index < cells.Length; index++)
        cells[index] = table.Share(Fresh($"ACCT-{index:D5}"));

      for (var index = 1; index < cells.Length; index++)
        Assert.NotSame(cells[0].GetString(), cells[index].GetString());

      var statistics = table.Snapshot();

      Assert.Equal(20, statistics.DistinctValues);
      Assert.Equal(0, statistics.Hits);
      Assert.Equal(0, statistics.EstimatedBytesSaved);
      Assert.False(statistics.AtCapacity);
    }

    [Fact]
    public void EveryKindButTextIsHandedBackExactlyAsItArrived()
    {
      // Strings only. Every other kind is inline in the 24-byte CellValue and has no heap object to
      // share, so the table must pass it through untouched and enter nothing on its account.
      //
      // Two of these cannot arrive through the spreadsheet door at all — a reader hands the adapter a
      // double for every numeric cell, and an error's literal is kept only when the canonical
      // spelling does not name it — but Share is reachable from any row source, so both are pinned
      // here rather than assumed away. The exact decimal is the one with something to lose: it is the
      // part of a number a round trip through a double would quietly drop.
      var table = new StringInterner(64);

      var values = new[]
      {
        CellValue.Blank,
        CellValue.Of(1.5),
        CellValue.Of(12.34m),
        CellValue.Of(7),
        CellValue.Of(long.MaxValue),
        CellValue.Of(new DateTime(2026, 3, 31)),
        CellValue.Of(true),
        CellValue.OfError(CellError.Value),
        CellValue.OfError(CellError.Other, "#SPILL!")
      };

      // Twice each, so a table that had entered one of them would show it as a hit on the second.
      foreach (var value in values)
      {
        Assert.Equal(value, table.Share(value));
        Assert.Equal(value, table.Share(value));
      }

      Assert.Equal(12.34m, table.Share(CellValue.Of(12.34m)).GetDecimal());

      var statistics = table.Snapshot();

      Assert.Equal(0, statistics.DistinctValues);
      Assert.Equal(0, statistics.Hits);
    }

    // --- The two guards --------------------------------------------------------------------------

    [Fact]
    public void PastTheCapTheTableStopsGrowingRatherThanFailing()
    {
      // Degradation, never failure, and the two halves of it in one place: what was already in the
      // table goes on being shared, and a value first met afterwards keeps the instance it arrived
      // with. Equality is untouched either way, which is what makes the cap a memory knob rather than
      // a correctness one.
      var table = new StringInterner(3);

      foreach (var value in new[] { "one", "two", "three" })
        table.Share(Fresh(value));

      var statistics = table.Snapshot();

      Assert.Equal(3, statistics.DistinctValues);
      Assert.Equal(3, statistics.Capacity);
      Assert.True(statistics.AtCapacity);

      Assert.Same(table.Share(Fresh("two")).GetString(), table.Share(Fresh("two")).GetString());

      var late = table.Share(Fresh("four"));
      var alsoLate = table.Share(Fresh("four"));

      Assert.NotSame(late.GetString(), alsoLate.GetString());
      Assert.Equal(late, alsoLate);
      Assert.Equal("four", alsoLate.GetString());

      // The value met after the cap did not take an entry either — the table stopped growing, it did
      // not start evicting.
      Assert.Equal(3, table.Snapshot().DistinctValues);
    }

    [Fact]
    public void ACapOfZeroSharesNothingAndReportsItselfAtCapacity()
    {
      // Sharing turned off. AtCapacity is true here deliberately: it is the one form of "at capacity"
      // a caller asked for, and reporting a table that will never grow as though it might is the
      // worse of the two readings.
      var table = new StringInterner(0);

      var first = table.Share(Fresh("Alpha Fund"));
      var second = table.Share(Fresh("Alpha Fund"));

      Assert.NotSame(first.GetString(), second.GetString());
      Assert.Equal(first, second);

      var statistics = table.Snapshot();

      Assert.Equal(0, statistics.Capacity);
      Assert.Equal(0, statistics.DistinctValues);
      Assert.Equal(0, statistics.Hits);
      Assert.True(statistics.AtCapacity);
    }

    [Fact]
    public void AStringAtTheLengthGuardIsSharedAndOneCharacterLongerIsNot()
    {
      // The boundary, both sides of it. 256 characters sits well above every label, code and category
      // a sheet repeats and well below the memo fields that would fill the table with entries that
      // never hit — so the guard is exclusive at 256 and the test says which side each neighbour
      // falls on rather than that "a long string is different".
      Assert.Equal(256, StringInterner.MaximumLength);

      var table = new StringInterner(64);
      var atGuard = new string('a', StringInterner.MaximumLength);
      var pastGuard = new string('b', StringInterner.MaximumLength + 1);

      Assert.Same(table.Share(Fresh(atGuard)).GetString(), table.Share(Fresh(atGuard)).GetString());
      Assert.NotSame(table.Share(Fresh(pastGuard)).GetString(), table.Share(Fresh(pastGuard)).GetString());

      // One entry, not two: the long one was never entered, so it did not occupy a slot it could
      // never score a hit in — which is the guard's actual purpose.
      var statistics = table.Snapshot();

      Assert.Equal(1, statistics.DistinctValues);
      Assert.Equal(1, statistics.Hits);
    }

    // --- The counters ----------------------------------------------------------------------------

    [Fact]
    public void HitsCountTheCellsHandedAnInstanceTheTableAlreadyHeld()
    {
      // Six cells over three distinct values: three first sightings and three duplicates that did not
      // have to be retained. Pinned to an exact number rather than to "greater than zero", because a
      // counter whose value nobody has checked is a counter that can quietly start answering a
      // different question.
      var table = new StringInterner(1024);

      foreach (var value in new[] { "Alpha", "Beta", "Alpha", "Gamma", "Alpha", "Beta" })
        table.Share(Fresh(value));

      var statistics = table.Snapshot();

      Assert.Equal(3, statistics.DistinctValues);
      Assert.Equal(3, statistics.Hits);
    }

    [Fact]
    public void EstimatedBytesSavedIsWhatTheDuplicatesWouldHaveOccupied()
    {
      // The estimator models the string layout exactly, so it is pinned exactly: 16 bytes of object
      // header and method-table pointer, 4 for the length, two per character plus a terminator, all
      // rounded up to the runtime's eight-byte granularity. "Alpha" is five characters — 16 + 4 + 12
      // = 32, already a multiple of eight — and "Distribution" is twelve, 16 + 4 + 26 = 46 rounded to
      // 48. One hit of each is 80 bytes that did not survive the fill.
      //
      // It is an estimate of the LAYOUT and named so: it does not know what the collector would have
      // done with the bytes, and it is not a measurement of a live set.
      var table = new StringInterner(1024);

      foreach (var value in new[] { "Alpha", "Distribution", "Alpha", "Distribution" })
        table.Share(Fresh(value));

      var statistics = table.Snapshot();

      Assert.Equal(2, statistics.Hits);
      Assert.Equal(80, statistics.EstimatedBytesSaved);
    }

    [Fact]
    public void TheOneLineFormOmitsTheCapacityClauseWhileThereIsRoomLeft()
    {
      var table = new StringInterner(1024);

      foreach (var value in new[] { "Alpha", "Beta", "Alpha", "Beta" })
        table.Share(Fresh(value));

      var rendered = table.Snapshot().ToString();

      Assert.Equal("shared 2 | distinct 2/1,024 | saved ~64B (estimated)", rendered);
      Assert.DoesNotContain("AT CAPACITY", rendered);
    }

    [Fact]
    public void TheOneLineFormNamesTheCapacityOnceTheTableHasFilled()
    {
      // The conditional clause, on the same footing as every other one in this vocabulary: it is the
      // one thing here a caller would act on, and a run that never reached the cap has nothing to say.
      var table = new StringInterner(2);

      foreach (var value in new[] { "Alpha", "Beta", "Alpha", "Beta" })
        table.Share(Fresh(value));

      Assert.Equal(
        "shared 2 | distinct 2/2 | saved ~64B (estimated) | AT CAPACITY",
        table.Snapshot().ToString());
    }

    [Fact]
    public void TheCountersOutliveTheEntries()
    {
      // What Release is for, and the reason DistinctValues is documented as the count REACHED rather
      // than the entries alive: dropping the strings must not drop the story of what reading them
      // cost, because a caller totalling up an import reads these after the workbook is gone.
      var table = new StringInterner(64);

      table.Share(Fresh("Alpha Fund"));
      table.Share(Fresh("Alpha Fund"));

      var before = table.Snapshot();

      table.Release();

      var after = table.Snapshot();

      Assert.Equal(1, before.DistinctValues);
      Assert.Equal(1, before.Hits);
      Assert.Equal(before.DistinctValues, after.DistinctValues);
      Assert.Equal(before.Hits, after.Hits);
      Assert.Equal(before.EstimatedBytesSaved, after.EstimatedBytesSaved);
    }

    // --- Through the window ------------------------------------------------------------------------

    [Fact]
    public void EqualCellsShareAcrossAChunkBoundary()
    {
      // A chunk is filled from the reader a chunk at a time, so cells in different chunks are adapted
      // by different fills. Sharing that stopped at a chunk edge would leave a sheet holding one
      // instance per chunk of every repeated caption — which is most of the saving, on the sheets
      // this exists for.
      //
      // The store here is built WITHOUT a table, which is the other half of the claim: a store that
      // was handed none makes its own at the default cap, so there is no configuration under which a
      // chunk fill hands out avoidable duplicates.
      var store = Store(new FakeRowSource(Repeating("Data", 20, 1)), rows: 20, columns: 1, chunkRows: 5, windowChunks: 4);

      var first = store.GetCell(0, 0, 0, 1).GetString();
      var acrossTheBoundary = store.GetCell(0, 9, 9, 1).GetString();

      Assert.Same(first, acrossTheBoundary);
      Assert.Equal(2, store.Snapshot().ChunkLoads);   // two fills, so the two cells really were adapted apart
    }

    [Fact]
    public void AChunkReadAgainAfterEvictionRejoinsTheValuesTheFirstParseCanonicalised()
    {
      // The case the table is scoped to the book FOR. A chase reader re-parsing rows the window has
      // dropped builds fresh strings for them, and without a table that outlives the window each
      // reload would start a second family of instances for values the first parse had already
      // canonicalised — so a declaration that reaches backwards would pay in retention for every
      // pass. The reload is real: the counter says so.
      var store = Store(new FakeRowSource(Repeating("Data", 60, 1)), rows: 60, columns: 1, chunkRows: 5, windowChunks: 4);

      var first = store.GetCell(0, 0, 0, 1).GetString();

      for (var row = 0; row < 60; row++)
        _ = store.GetCell(0, row, row, 1);

      var afterTheReload = store.GetCell(0, 0, 0, 1).GetString();

      Assert.Equal(1, store.Snapshot().ChunkReloads);
      Assert.Same(first, afterTheReload);
    }

    [Fact]
    public void TheSheetsOfOneWorkbookShareOneTable()
    {
      // Captions, codes and categories repeat ACROSS the sheets of a book, so a table per sheet would
      // hold one copy of every one of them per sheet. One table for the book is what the counters
      // describe, which is why they hang off the workbook and not off Statistics(sheet).
      using var book = Book(new FakeRowSource(
        Repeating("Summary", 3, 1, "Alpha Fund"),
        Repeating("Detail", 3, 1, "Alpha Fund")));

      var summary = book.Sheet("Summary");
      var detail = book.Sheet("Detail");

      Assert.Same(summary[0, 0].GetString(), detail[0, 0].GetString());

      // One distinct value for two sheets of three cells each: five of the six joined the first.
      var statistics = book.InterningStatistics;

      Assert.Equal(1, statistics.DistinctValues);
      Assert.Equal(5, statistics.Hits);
    }

    [Fact]
    public void ASheetsOwnStatisticsSayNothingAboutSharing()
    {
      // Deliberate, and worth an assertion because the alternative is so tempting. The table is the
      // book's, so a per-sheet share count would be a number that does not add up — two sheets'
      // figures could not be summed, and neither could be read on its own. The sheet's one-line form
      // is therefore byte-for-byte what it was before sharing existed (see StreamingStatisticsTests,
      // whose pinned renders this change does not touch), and the book's line is where the story is.
      using var book = Book(new FakeRowSource(Repeating("Data", 8, 2)));

      _ = book.Sheet("Data")[0, 0];

      var sheetLine = book.Statistics("Data")!.Value.ToString();

      Assert.DoesNotContain("shared", sheetLine);
      Assert.DoesNotContain("distinct", sheetLine);
      Assert.Contains("shared", book.InterningStatistics.ToString());
    }

    // --- The knob ----------------------------------------------------------------------------------

    [Fact]
    public void TheDefaultCapIsGenerousEnoughForARealWorkbooksVocabulary()
    {
      // 65,536: hundreds or a few thousand distinct values per sheet and tens of thousands across a
      // large multi-sheet book, so the default holds every one of them for nearly every real file. It
      // is quoted in the option's own documentation, so it is pinned rather than left to drift.
      Assert.Equal(65_536, new WorkbookOptions().MaxInternedStrings);
    }

    [Fact]
    public void ACapOfZeroTurnsSharingOffForTheWholeBook()
    {
      // The knob at its off position, for measurement or for a workbook whose text is known to be
      // unique. What must not change is the cells: they are equal, they are the same kind, and only
      // their identity differs — the same promise sharing makes, read backwards.
      using var book = Book(
        new FakeRowSource(Repeating("Data", 4, 1)),
        new WorkbookOptions { WarmReaders = false, MaxInternedStrings = 0 });

      var sheet = book.Sheet("Data");

      Assert.NotSame(sheet[0, 0].GetString(), sheet[0, 1].GetString());
      Assert.Equal(sheet[0, 0], sheet[0, 1]);

      var statistics = book.InterningStatistics;

      Assert.Equal(0, statistics.Capacity);
      Assert.Equal(0, statistics.DistinctValues);
      Assert.Equal(0, statistics.Hits);
      Assert.True(statistics.AtCapacity);
    }

    [Fact]
    public void ACapReachedByARealSheetIsReportedAsReached()
    {
      // Four distinct values against a cap of two: the table takes the two it met first and reports
      // itself full. The signal is genuinely two-way — raise the cap when the vocabulary that
      // repeats is larger than it, but lower it when, as here, the column filling it never repeats
      // and every entry it holds for the book's life is one that will never score a hit.
      using var book = Book(
        new FakeRowSource(new FakeSheet("Data", 4, 1, (_, row) => Fresh($"Fund {row}"))),
        new WorkbookOptions { WarmReaders = false, MaxInternedStrings = 2 });

      var sheet = book.Sheet("Data");

      for (var row = 0; row < 4; row++)
        Assert.Equal($"Fund {row}", sheet[0, row].GetString());

      var statistics = book.InterningStatistics;

      Assert.Equal(2, statistics.DistinctValues);
      Assert.Equal(2, statistics.Capacity);
      Assert.True(statistics.AtCapacity);
    }

    [Fact]
    public void ANegativeCapIsRefusedWhereEveryOtherOptionIs()
    {
      // Zero is the off switch, so a negative cannot be quietly read as one: it is an argument bug,
      // and it fails at Open with the rest of the option validation rather than at the first text
      // cell of the first sheet.
      var failure = Assert.Throws<ArgumentOutOfRangeException>(() => Workbook.Open(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "repeated-text.xlsx"),
        new WorkbookOptions { MaxInternedStrings = -1 }));

      Assert.Equal("MaxInternedStrings", failure.ParamName);
      Assert.Contains("pass 0 to share nothing", failure.Message);
    }

    // --- Lifetime ------------------------------------------------------------------------------------

    [Fact]
    public void TheInterningFiguresAreReadableAfterTheWorkbookIsDisposed()
    {
      // On the same footing as Statistics(sheet) and for the same reason: the numbers describe reading
      // that has already happened, and a caller totalling up what an import cost should not have to
      // keep the workbook alive to do it. DistinctValues in particular must not fall to zero when the
      // entries are dropped — it is the count the table reached, not the entries alive at the moment
      // of asking.
      var book = Book(new FakeRowSource(Repeating("Data", 6, 2)));

      _ = book.Sheet("Data")[0, 0];

      var before = book.InterningStatistics;

      Assert.Equal(1, before.DistinctValues);
      Assert.Equal(11, before.Hits);

      book.Dispose();

      var after = book.InterningStatistics;

      Assert.Equal(before.DistinctValues, after.DistinctValues);
      Assert.Equal(before.Hits, after.Hits);
      Assert.Equal(before.EstimatedBytesSaved, after.EstimatedBytesSaved);
      Assert.Equal(before.Capacity, after.Capacity);

      // ...while the workbook itself is as closed as it ever was.
      Assert.Throws<ObjectDisposedException>(() => book.Sheet("Data"));
    }

    [Fact]
    public void DisposingAWorkbookLetsGoOfTheStringsItWasHolding()
    {
      // The other half of Dispose, and the one that cannot be read off a counter. The table outlives
      // the window by design — that is what makes a reload rejoin the first parse's values — so it
      // must not outlive the workbook, or a caller holding a disposed book to read the figures above
      // would still be pinning every distinct string of it.
      //
      // The workbook is opened, read and disposed inside a method of its own so that when the
      // collector runs there is no stack slot left holding the cell: a local in this frame would keep
      // it alive under a debug build and the assertion would be about the JIT rather than about
      // Release.
      var shared = ReadThenDispose();

      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      Assert.False(shared.IsAlive, "the disposed workbook is still pinning the strings it shared");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReadThenDispose()
    {
      var book = Book(new FakeRowSource(Repeating("Data", 6, 2)));
      var reference = new WeakReference(book.Sheet("Data")[0, 0].GetString());

      book.Dispose();

      return reference;
    }
  }
}
