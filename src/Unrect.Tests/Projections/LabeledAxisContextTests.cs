using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Step 1 of labelled axes: a table publishes its columns as an ambient <see cref="LabelScope"/> on
  /// the <see cref="ProjectionContext"/>, and a row resolves a caption by translating the ordinal from
  /// the frame the header was read in to the row's own frame, then bounds-checking. In the built-in
  /// table the translation is the identity (a body band shares the table's origin), so the first two
  /// pins reach for the seam directly — a scope captured at one frame, a row read at another — because
  /// that offset is what step 2 introduces and what the whole model rests on not getting wrong.
  /// </summary>
  public class LabeledAxisContextTests
  {
    /// <summary>Header <c>X Y Amount Z</c> over one body row <c>1 2 100 999</c> — Amount at column 2, its neighbour Z at 3.</summary>
    private static ISpace AmountAtColumnTwo() => Mixed(new object?[,]
    {
      { "X", "Y", "Amount", "Z" },
      { 1m,  2m,  100m,     999m },
    });

    private static TableView TableOver(ISpace sheet) => Table((TableView view) => view).Map(sheet);

    // --- 1. CRITICAL: the capture->reading translation is actually applied ----------------------------
    //
    // The table captures its columns at origin width 0, so "Amount" is ordinal 2. We then read a row
    // whose own frame is one column to the right (origin width 1) over the same sheet. The label must
    // still land on the absolute Amount cell (C2 = 100), which means the local index is ordinal - 1 = 1,
    // not the bare ordinal 2. If the `CaptureOrigin.Width - Origin.Width` term were dropped, index 2
    // would be read instead — the neighbour Z (999). row.Decimal(2) is that neighbour, proved distinct.

    [Fact]
    public void ACaptionResolvesToTheAbsoluteColumnWhenTheRowFrameIsOffsetFromTheCapture()
    {
      var sheet = AmountAtColumnTwo();
      var table = TableOver(sheet);

      // The reading frame: the body row (row 1) shifted one column right of the capture frame.
      var reading = table.Context.Advance(new Offset(1, 1));
      var strip = new CellStrip(sheet.GetSubspace(new Offset(1, 1), new Area(3, 1)), Orientation.Horizontal, reading);
      var row = new TableRow(0, strip, reading);

      // Translation applied: "Amount" lands on the absolute Amount cell, C2, holding 100.
      Assert.Equal(100m, row.Decimal("Amount"));
      Assert.Equal("C2", row.AddressOf("Amount").A1);

      // The bare ordinal (2) is the neighbour Z (999) in this frame — what dropping the term would read.
      Assert.Equal(999m, row.Decimal(2));
      Assert.NotEqual(row.Decimal("Amount"), row.Decimal(2));
    }

    // --- 2. A label whose column has narrowed out of the row is a clean, absorbable MissingLabel -------
    //
    // Same capture (Amount at ordinal 2), but the row is only two columns wide. The translated index
    // (2) is past the row, so the label has departed: a plain ProjectionException with a path and an
    // A1, absorbable (IsFault false) — never an OutOfBoundsException, never a silent neighbour read.

    [Fact]
    public void ADepartedLabelIsAnAbsorbableFailureNotAnOutOfBoundsRead()
    {
      var sheet = AmountAtColumnTwo();
      var table = TableOver(sheet);

      var reading = table.Context;
      var strip = new CellStrip(sheet.GetSubspace(new Offset(0, 1), new Area(2, 1)), Orientation.Horizontal, reading);
      var row = new TableRow(0, strip, reading);

      var failure = Assert.Throws<ProjectionException>(() => row.Decimal("Amount"));

      Assert.Contains("column 'Amount' is not in this region", failure.Message);
      Assert.False(failure.IsFault);
      Assert.NotEqual(0, failure.Path.Length);
      Assert.NotEqual(0, failure.Location.A1.Length);
    }

    // --- 2b. A label to the LEFT of an offset row is also a clean MissingLabel (the local < 0 branch) --
    //
    // The mirror of pin 2, exercising the other half of the bounds-check. "X" is captured at ordinal 0,
    // but the row is read two columns to the right (origin width 2), so the translated index is negative
    // (0 - 2 = -2): the label sits left of this frame. Same absorbable failure as a label off the right
    // edge — never an OutOfBoundsException, never a silent read of whatever the negative index would hit.

    [Fact]
    public void ALabelLeftOfAnOffsetRowIsAlsoAnAbsorbableMissingLabel()
    {
      var sheet = AmountAtColumnTwo();
      var table = TableOver(sheet);

      // The reading frame is the last two columns (C, D); "X" (column A) is to their left.
      var reading = table.Context.Advance(new Offset(2, 1));
      var strip = new CellStrip(sheet.GetSubspace(new Offset(2, 1), new Area(2, 1)), Orientation.Horizontal, reading);
      var row = new TableRow(0, strip, reading);

      var failure = Assert.Throws<ProjectionException>(() => row.Decimal("X"));

      Assert.Contains("column 'X' is not in this region", failure.Message);
      Assert.False(failure.IsFault);
    }

    // --- 3. Byte-identical: the rerouted funnel keeps the absent/ambiguous/headerless wording ----------

    [Fact]
    public void AnAbsentColumnStillListsTheColumnsTheTableDoesCarry()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Amount" },
        { "Acme", 10m },
      });

      var failure = Assert.Throws<ProjectionException>(
        () => Table((TableRow row) => row.Decimal("Net")).Map(sheet));

      Assert.Contains("there is no column named 'Net'; available columns: 'Investor', 'Amount'.", failure.Message);
    }

    [Fact]
    public void AnAmbiguousColumnStillNamesTheIndicesAndDefersToTheIndex()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Amount", "Amount" },
        { 10m, 20m },
      });

      var failure = Assert.Throws<ProjectionException>(
        () => Table((TableRow row) => row.Decimal("Amount")).Map(sheet));

      Assert.Contains("column 'Amount' appears at indices 0 and 1; use the index.", failure.Message);
    }

    // --- 4. Engine seam: the pushed scope survives the engine minting child contexts through a composite

    private sealed record Line(string Investor, decimal Amount);

    [Fact]
    public void ACaptionResolvesThroughATableNestedInsideALayout()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Amount" },
        { "Acme", 10m },
        { "Beta", 20m },
      });

      // One composite (the flow) above the table: the engine descends into the table, the table pushes
      // its scope, and the body bands advance from there — the whole chain must carry the labels.
      IReadOnlyList<Line> lines = VerticalFlow(v =>
        v.Next(Table((TableRow row) => new Line(row.Text("Investor"), row.Decimal("Amount"))))).Map(sheet);

      Assert.Equal(new[] { new Line("Acme", 10m), new Line("Beta", 20m) }, lines);
    }

    // --- 5. headerRows: 0 pushes no scope, so a by-name read is headerless, exactly as before ----------

    [Fact]
    public void AHeaderlessTablePushesNoScopeAndAByNameReadReportsSo()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Acme", 10m },
        { "Beta", 20m },
      });

      var failure = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 0, project: (TableRow row) => row.Decimal("Amount")).Map(sheet));

      Assert.Contains("the table was declared without a header row; use column indices.", failure.Message);
    }
  }
}
