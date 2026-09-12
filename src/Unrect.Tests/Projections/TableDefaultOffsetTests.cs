using System.Collections.Generic;
using System.Linq;

using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The node-type placement default for the leaf <c>Table</c>. Table's default offset moved from
  /// <see cref="OffsetStrategies.SkipBlankRows"/> (skip leading blank <em>rows</em>, column always 0)
  /// to <see cref="OffsetStrategies.SkipToFirstNonBlankCell"/> (down to the first content row, then
  /// across to its first non-blank cell), so a self-contained region leaf self-locates on BOTH axes.
  /// <para>
  /// The change is identical to the old offset whenever the content starts at column 0 (the whole
  /// committed corpus); it diverges only when a table's first content row starts past column 0 — the
  /// self-location the change exists to enable (pinned as the headline in
  /// <c>LabeledAxisPrimitivesTests.ADefaultTableNowSelfLocatesOntoAColumnIndentedRegion</c>). These
  /// pins cover the col-0 before/after identity, the accepted ragged residual, the orthogonality with
  /// <c>onBlank</c>, and that the default stays lazy (row-at-a-time).
  /// </para>
  /// </summary>
  public class TableDefaultOffsetTests
  {
    private sealed record Line(string Name, decimal Amount);

    // --- 1. col-0 before/after identity ------------------------------------------------------------

    [Fact]
    public void OnATopLeftAlignedTableTheNewDefaultDenotesIdenticallyToTheOldSkipBlankRowsOffset()
    {
      // For any col-0 sheet SkipToFirstNonBlankCell and SkipBlankRows land the same origin, so the
      // whole change is a no-op there. Stated once explicitly for the record: the bare Table (new
      // default) and a twin whose default offset is REPLACED by the outgoing SkipBlankRows() denote
      // L3-identically (value, extent consumed and from where, advance, diagnostics, and any failure's
      // path). Every other green test on a col-0 sheet is a further instance of this identity.
      var sheet = Mixed(new object?[,]
      {
        { "Name", "Amount" },
        { "Acme", 10m },
        { "Beta", 20m },
      });

      var newDefault = Table(r => new Line(r.Text("Name"), r.Decimal("Amount")));
      var oldOffset = OffsetBy(OffsetStrategies.SkipBlankRows())
        .Of(Table(r => new Line(r.Text("Name"), r.Decimal("Amount"))));

      AssertL3(Observe(oldOffset, sheet), Observe(newDefault, sheet));
    }

    // --- 2. the ragged residual, pinned as EXPECTED (not a bug) ------------------------------------

    [Fact]
    public void ARaggedHeaderlessTableLandsAtTheFirstRowsCornerAndLosesTheLeftPart_TheDocumentedMiss()
    {
      // The accepted residual (spec §2.2, mirroring the strategy-level pin
      // OffsetStrategyTests.SkipToFirstNonBlankCell_OnARaggedRegion_...): the offset finds the FIRST
      // content row's corner, which is the region's true corner only when it is top-left-aligned.
      // Here the first content row starts at column 1, but a lower row reaches back to column 0 — so
      // the origin lands at (1, 0), the discovered block spans column 1 only, and the lower-left 99m
      // is orphaned rather than read.
      //
      // This is EXPECTED, not a bug, and the node-type split does not fix it. A ragged table's escape
      // hatch is an explicit offset (e.g. Right(n)/OffsetBy) or the eager skip-blank-rows-and-columns
      // spelling — where a headerless, non-top-left table is meant to go.
      var ragged = Mixed(new object?[,]
      {
        { null, 10m },     // r0: first content cell at column 1 (C0 = 1)
        { 99m, 20m },      // r1: reaches back to column 0, LEFT of the first row's corner
      });

      IReadOnlyList<decimal> read = Table(0, r => r.Decimal(0)).Map(ragged);

      // Column 1 read for both rows; the 99m in column 0 is lost. A non-ragged read would have
      // included it — the miss is silent by design, which is why it is pinned here.
      Assert.Equal(new[] { 10m, 20m }, read);
    }

    // --- 3. orthogonality with onBlank -------------------------------------------------------------

    [Fact]
    public void TheLeadingOffsetComposesWithAnInteriorOnBlankSkip()
    {
      // The offset is the table's LEADING placement (the top-left corner); onBlank governs blank rows
      // INTERIOR/trailing to the body via the height rule. They act on different axes at different
      // times (spec §3.1), so a col>0 table with an interior blank both self-locates its corner AND
      // skips the interior blank. Column A is entirely blank; the header and body start at column B.
      var sheet = Mixed(new object?[,]
      {
        { null, "Name", "Amount" },   // header, C0 = 1
        { null, "Alpha", 100m },
        { null, null, null },         // interior blank row
        { null, "Gamma", 300m },
      });

      var records = Table(r => new Line(r.Text("Name"), r.Decimal("Amount")), onBlank: BlankRowStrategy.Skip)
        .Map(sheet);

      // Corner located (Name/Amount bind at columns B/C) and the interior blank skipped, not stopped
      // at — the column shift and the blank-skip are independent.
      Assert.Equal(new[] { new Line("Alpha", 100m), new Line("Gamma", 300m) }, records);
    }

    // --- 4. laziness unchanged (the default stays row-at-a-time) -----------------------------------
    //
    // SkipToFirstNonBlankCell's own laziness is pinned at the strategy level
    // (OffsetStrategyTests.SkipToFirstNonBlankCell_WithLeadingBlankRows_TouchesOnlyUpToTheFirstContentRow),
    // and Skip run-to-edge is pinned in TableBlankRowStrategyTests. The new signal here: as the
    // DEFAULT offset on the DEFAULT (Stop / DiscoveredBlock) table — the common case — the offset scan
    // does not force the sheet up front; the first record projects having touched only the header and
    // its own row, exactly as the outgoing SkipBlankRows did (spec §4: rows-touched is unchanged).

    [Fact]
    public void TheDefaultStopTableStillPeeksOneRowAtATimeUnderTheNewOffset()
    {
      var values = new object?[11, 2];

      values[0, 0] = "Name";
      values[0, 1] = "Amount";

      for (var row = 1; row <= 10; row++)
      {
        values[row, 0] = $"row {row}";
        values[row, 1] = row * 10m;
      }

      var counter = new CountingSpace(Mixed(values));
      var observations = new List<int>();

      // No onBlank — the default Stop / DiscoveredBlock path, whose offset is now SkipToFirstNonBlankCell.
      Table(r => { observations.Add(counter.RowsTouched); return r.Index; }).Apply(counter);

      // Ten body records, each projected in step with the walk.
      Assert.Equal(10, observations.Count);

      // The first record projects having read only the header and its own row: two, not the eleven an
      // up-front measurement (or an offset scan that read the whole extent) would have forced. This is
      // the no-regression claim — the new offset touches the same leading rows the old one did.
      Assert.Equal(2, observations[0]);

      // Monotonic, and by the last record the whole sheet has been peeked.
      Assert.Equal(observations.OrderBy(count => count).ToList(), observations);
      Assert.Equal(11, counter.RowsTouched);
    }
  }
}
