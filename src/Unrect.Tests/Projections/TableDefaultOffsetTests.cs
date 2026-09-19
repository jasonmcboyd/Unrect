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
  /// A <c>Table</c> declares no offset of its own: like anything else it starts past the blank
  /// spans in front of it, at the left edge of what it is handed. Columns its header leaves blank on
  /// the way to the first caption are columns of the table with no label. These pins cover the
  /// agreement with <see cref="OffsetStrategies.SkipBlankRows"/>, the ragged table that keeps its
  /// left part, and the orthogonality with <c>onBlank</c>.
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
    public void ARaggedHeaderlessTableKeepsItsLeftPart()
    {
      // Once a documented miss: the table used to step across to its first row's first non-blank
      // cell, so a lower row that reached back to column 0 lost what it had there, silently. A table
      // starts at the left edge of what it is handed, so the column is part of it — blank in the
      // first row, 99 in the second — and nothing is dropped.
      var ragged = Mixed(new object?[,]
      {
        { null, 10m },     // r0: first content cell at column 1
        { 99m, 20m },      // r1: reaches back to column 0
      });

      IReadOnlyList<(decimal? Left, decimal Right)> read = Table(0, r => (r[0].DecimalOrBlank(), r[1].Decimal())).Map(ragged);

      Assert.Equal(new (decimal?, decimal)[] { (null, 10m), (99m, 20m) }, read);

      // A declaration that wants the old landing says so, and gets it — with the old miss.
      Assert.Equal(
        new[] { 10m, 20m },
        SkipToFirstNonBlankCell().Of(Table(0, r => r[0].Decimal())).Map(ragged));
    }

    [Fact]
    public void AValueFurtherDownALeadingColumnDoesNotMoveWhereTheTableBegins()
    {
      // The lead — the columns a header leaves blank before its first caption — is settled by the
      // first row alone. A stray label in column A three rows down is a cell of an unlabeled column,
      // not a reason for the table to become one column wide.
      var sheet = Mixed(new object?[,]
      {
        { null, "Name", "Amount" },
        { null, "Acme", 10m },
        { "Total", null, 10m },
      });

      Assert.Equal(new[] { "", "Name", "Amount" }, Table(t => t.ColumnNames).Map(sheet));
      Assert.Equal(new[] { 10m, 10m }, Table(r => r["Amount"].Decimal()).Map(sheet));
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
