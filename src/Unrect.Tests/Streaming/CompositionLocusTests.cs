using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// What the streaming store is told when a strategy composes with another one — and the one place
  /// these counters are <em>chosen</em> rather than inherited from the reading.
  /// <para>
  /// The store tells a sweep of a bounded band apart from a walk down the sheet by being told which
  /// band is open. Until phase 6 it was told by inference: a windowed view handed its own extent
  /// down with every cell it read, so the band was whatever subspace object happened to make the
  /// read. That made a composing STRATEGY a hazard — <c>RowsThenColumns</c> narrows its region by
  /// arithmetic rather than cutting a second object, so a column scan reading two rows announced the
  /// band its PARENT object named, all 1,201 of them, and the store was told a sweep was open that
  /// was not.
  /// </para>
  /// <para>
  /// <b>Phase 6 removes the inference, and with it the hazard.</b> The engine announces once per
  /// placement (<c>ISweepAware</c>), and what it announces is the placement's own region — the
  /// rectangle the declaration asked for — so a strategy's reads announce nothing at all and the
  /// composition's spelling cannot reach the counters. The first case below is therefore the one
  /// that moved: its overrun was an artefact of the inference and is gone. The second did not move,
  /// because a declaration that really does open a 1,201-row band really does overrun a 256-row
  /// window, which is the honest reading the counter exists for.
  /// </para>
  /// <para>
  /// <b>What is still chosen, and still recorded here.</b> The answers do not move — the same
  /// declaration reads the same extent through either door — so nothing else in the suite can see
  /// what the store was told; only the counters can, and only here. That is as true of a number that
  /// is now zero as of one that is not.
  /// </para>
  /// <para>
  /// The window and chunk sizes mirror <see cref="WorkbookTests"/>' tall-sheet gate on purpose, so
  /// the second case below can be read directly against the plain monotone walk pinned there.
  /// </para>
  /// </summary>
  public class CompositionLocusTests
  {
    private static string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary><paramref name="area"/> applied to the tall ledger through the streaming door: the extent it settled on.</summary>
    private static string Streamed(IAreaStrategy area)
    {
      using var book = Workbook.Open(Path("tall-ledger.xlsx"));

      return Sized(area).Of(Range(block => $"{block.Width}x{block.Height}")).Map(book.Sheet("Ledger"));
    }

    /// <summary>The same declaration through the eager door, where there is no window to announce anything to.</summary>
    private static string Eager(IAreaStrategy area)
      => Sized(area).Of(Range(block => $"{block.Width}x{block.Height}")).Map(
        SpreadsheetSpace.Create(Path("tall-ledger.xlsx"), "Ledger"));

    /// <summary>
    /// Two rows taken without reading anything, then the columns measured inside them — the
    /// composition where the first half consumes no rows at all, so every read the store sees comes
    /// from the second half and carries the parent's band.
    /// </summary>
    private static IAreaStrategy FirstHalfReadsNothing()
      => AreaStrategies.RowsThenColumns(RowStrategies.TakeRows(2), ColumnStrategies.TakeColumnsWhileAnyValue());

    /// <summary>The contrast: a first half that walks the sheet itself, so the reading is its own.</summary>
    private static IAreaStrategy FirstHalfReads()
      => AreaStrategies.RowsThenColumns(RowStrategies.TakeRowsWhileAnyValue(), ColumnStrategies.TakeColumnsWhileAnyValue());

    [Theory]
    [InlineData("first-half-reads-nothing", "3x2")]
    [InlineData("first-half-reads", "3x1201")]
    public void AndTheAnswerIsTheSameThroughEitherDoor(string composition, string extent)
    {
      // The two doors settle on exactly the same rectangle: a composed area strategy whose first
      // half reads nothing, or reads the whole sheet, means the same through a stream as whole.
      var area = composition == "first-half-reads-nothing" ? FirstHalfReadsNothing() : FirstHalfReads();

      Assert.Equal(extent, Eager(area));
      Assert.Equal(extent, Streamed(area));
    }
  }
}
