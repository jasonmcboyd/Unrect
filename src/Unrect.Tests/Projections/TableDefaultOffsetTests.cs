using System.Collections.Generic;

using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A <c>Table</c>'s default offset is <see cref="OffsetStrategies.SkipToFirstNonBlankCell"/>: down
  /// to the first content row, then across to its first non-blank cell, so a table self-locates on
  /// both axes. On a table whose content starts at column 0 it lands where
  /// <see cref="OffsetStrategies.SkipBlankRows"/> would; it differs only when the first content row
  /// starts past column 0, which is the self-location it exists for (pinned as the headline in
  /// <c>LabeledAxisPrimitivesTests.ADefaultTableNowSelfLocatesOntoAColumnIndentedRegion</c>). These
  /// pins cover the column-0 agreement, the accepted ragged residual, and the orthogonality with
  /// <c>onBlank</c>.
  /// </summary>
  public class TableDefaultOffsetTests
  {
    private sealed record Line(string Name, decimal Amount);

    // --- 1. column-0 agreement ---------------------------------------------------------------------

    [Fact]
    public void OnATopLeftAlignedTableTheDefaultDenotesIdenticallyToSkipBlankRows()
    {
      // For any column-0 sheet SkipToFirstNonBlankCell and SkipBlankRows land the same origin: the
      // bare Table and a twin whose default offset is replaced by SkipBlankRows() denote
      // L3-identically (value, extent consumed and from where, advance, diagnostics, and any failure's
      // path).
      var sheet = Mixed(new object?[,]
      {
        { "Name", "Amount" },
        { "Acme", 10m },
        { "Beta", 20m },
      });

      var byDefault = Table(r => new Line(r["Name"].Text(), r["Amount"].Decimal()));
      var byBlankRows = OffsetBy(OffsetStrategies.SkipBlankRows())
        .Of(Table(r => new Line(r["Name"].Text(), r["Amount"].Decimal())));

      AssertL3(Observe(byBlankRows, sheet), Observe(byDefault, sheet));
    }

    // --- 2. the ragged residual, pinned as EXPECTED (not a bug) ------------------------------------

    [Fact]
    public void ARaggedHeaderlessTableLandsAtTheFirstRowsCornerAndLosesTheLeftPart_TheDocumentedMiss()
    {
      // The accepted residual (mirroring the strategy-level pin
      // OffsetStrategyTests.SkipToFirstNonBlankCell_OnARaggedRegion_...): the offset finds the FIRST
      // content row's corner, which is the region's true corner only when it is top-left-aligned.
      // Here the first content row starts at column 1, but a lower row reaches back to column 0 — so
      // the origin lands at (1, 0), the discovered block spans column 1 only, and the lower-left 99m
      // is orphaned rather than read.
      //
      // This is EXPECTED, not a bug. A ragged table's escape hatch is an explicit offset (Right(n),
      // OffsetBy) or SkipEmptyRowsAndColumns — where a headerless, non-top-left table is meant to go.
      var ragged = Mixed(new object?[,]
      {
        { null, 10m },     // r0: first content cell at column 1 (C0 = 1)
        { 99m, 20m },      // r1: reaches back to column 0, LEFT of the first row's corner
      });

      IReadOnlyList<decimal> read = Table(0, r => r[0].Decimal()).Map(ragged);

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
      // times, so a table past column 0 with an interior blank both self-locates its corner AND
      // skips the interior blank. Column A is entirely blank; the header and body start at column B.
      var sheet = Mixed(new object?[,]
      {
        { null, "Name", "Amount" },   // header, C0 = 1
        { null, "Alpha", 100m },
        { null, null, null },         // interior blank row
        { null, "Gamma", 300m },
      });

      var records = Table(r => new Line(r["Name"].Text(), r["Amount"].Decimal()), onBlank: BlankRowStrategy.Skip)
        .Map(sheet);

      // Corner located (Name/Amount bind at columns B/C) and the interior blank skipped, not stopped
      // at — the column shift and the blank-skip are independent.
      Assert.Equal(new[] { new Line("Alpha", 100m), new Line("Gamma", 300m) }, records);
    }
  }
}
