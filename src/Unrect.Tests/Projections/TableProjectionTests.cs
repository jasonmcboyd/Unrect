using System;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Table splits a discovered extent into an optional header row and the body beneath it. Two
  /// mapping tiers ship: by index, always; by name, when a header was declared. Every way of asking
  /// for a column that is not there produces a message that says what is.
  /// </summary>
  public class TableProjectionTests
  {
    private static ISpace SimpleTable() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10 },
      { "Beta", 20 },
      { "Gamma", 30 },
    });

    // --- The rung-five ambiguity, recorded ---------------------------------------------------------------
    //
    // The one overload cost of retiring TableRows* into the Table family, written down here because it
    // is why several sites in this file carry a type on a lambda parameter that reads as noise.
    //
    // The two rung-5 lambdas are Table(Func<TableRow, T>) and Table(Func<TableView, T>). A lambda body
    // that touches nothing distinctive is ambiguous between them, and the compiler says so:
    //
    //     Table(t => t)                 // CS0121: ambiguous between Table<T>(Func<TableRow, T>)
    //     Table(t => t.Location.A1)     //         and Table<T>(Func<TableView, T>)   -- both views
    //     Table(x => 0)                 //         have a Location, and neither is touched by 0
    //
    // Typing the parameter resolves it, and that is the whole workaround:
    //
    //     Table((TableView t) => t)             Table((TableRow r) => r.Location.A1)
    //
    // An untyped null argument is ambiguous for the same reason and one more — the binding overload
    // Table<T>(Func<TableBinding<T>, TableBinding<T>>) is a third candidate — so a cast stands in for
    // the type (see BothLambdaRungs_RejectANullProjection).
    //
    // Two things are worth stating about the shape of the cost, neither of which can be asserted, since
    // what they claim is the ABSENCE of a compile error:
    //
    //   * Every other member of the family resolves unaided. A lambda body that reaches for anything
    //     only one view has (row.Index, table.RowCount, r["Amount"], t.ColumnNames) picks its overload
    //     on its own, which is why the great majority of the sites below carry no type.
    //   * The bind rung, Table(1, captions => …), resolves against both of them without help: a
    //     CaptionMap's members exist on neither view, so the wrong candidates fail to bind and drop
    //     out. That is the case that mattered, and it is clean.
    //
    // The compiler names both candidates (CS0121) rather than reporting the useless CS0411, so a reader
    // who hits this is told what to do by the error itself. Recorded, priced, kept.

    // --- Defaults --------------------------------------------------------------------------------------

    [Fact]
    public void Table_SkipsLeadingBlankRowsByDefault()
    {
      var space = Mixed(new object?[,]
      {
        { null, null },
        { null, null },
        { "Investor", "Amount" },
        { "Acme", 10 },
      });

      var names = Table(t => t.ColumnNames).Map(space);

      Assert.Equal(new[] { "Investor", "Amount" }, names);
    }

    [Fact]
    public void Table_DiscoversItsExtentFromTheValuesPresent()
    {
      var space = Mixed(new object?[,]
      {
        { "Investor", "Amount", null },
        { "Acme", 10, null },
        { null, null, null },
        { "not part of the table", null, null },
      });

      var applied = Table(t => (t.ColumnCount, t.RowCount)).Apply(space);

      Assert.Equal((2, 1), applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void Table_SplitsTheHeaderFromTheBody()
    {
      var view = Table((TableView t) => t).Map(SimpleTable());

      Assert.True(view.HasHeader);
      Assert.Equal(2, view.ColumnCount);
      Assert.Equal(3, view.RowCount);
      Assert.Equal(3, view.Rows.Count);
      Assert.Equal(new[] { "Investor", "Amount" }, view.Header.Select(c => c.GetString()).ToArray());
    }

    [Fact]
    public void TableView_ExposesTheWholeExtentIncludingTheHeader()
    {
      var view = Table((TableView t) => t).Map(SimpleTable());

      Assert.Equal(2, view.Space.Area.Size.Width);
      Assert.Equal(4, view.Space.Area.Size.Height);
    }

    [Fact]
    public void Table_ConsumesItsWholeExtent()
    {
      var applied = Table(t => t.RowCount).Apply(SimpleTable());

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(4, applied.Consumed.Height);
    }

    // --- Header rows -----------------------------------------------------------------------------------------

    [Fact]
    public void Table_WithoutAHeader_TreatsEveryRowAsBody()
    {
      var view = Table(0, (TableView t) => t).Map(SimpleTable());

      Assert.False(view.HasHeader);
      Assert.Equal(4, view.RowCount);
      Assert.Empty(view.Header);
      Assert.Empty(view.ColumnNames);
    }

    [Fact]
    public void ColumnNames_HasOneEntryPerHeaderCell()
    {
      // ColumnNames mirrors the header strip, so it is empty exactly when there is no header.
      var withHeader = Table((TableView t) => t).Map(SimpleTable());
      var withoutHeader = Table(0, (TableView t) => t).Map(SimpleTable());

      Assert.Equal(withHeader.Header.Count, withHeader.ColumnNames.Count);
      Assert.Equal(withoutHeader.Header.Count, withoutHeader.ColumnNames.Count);
      Assert.Empty(withoutHeader.ColumnNames);
    }

    [Fact]
    public void ColumnNames_TrimsTextAndBlanksOutEverythingElse()
    {
      var space = Mixed(new object?[,]
      {
        { "  Amount  ", null, 7 },
        { 1, 2, 3 },
      });

      var names = Table(t => t.ColumnNames).Map(space);

      Assert.Equal(new[] { "Amount", "", "" }, names);
    }

    [Fact]
    public void Table_WithAHeaderRowOnly_HasNoBodyRows()
    {
      var space = Mixed(new object?[,] { { "Investor", "Amount" } });

      var view = Table((TableView t) => t).Map(space);

      Assert.True(view.HasHeader);
      Assert.Equal(0, view.RowCount);
      Assert.Empty(view.Rows);
      Assert.Equal(new[] { "Investor", "Amount" }, view.ColumnNames);
    }

    [Fact]
    public void Table_WithAHeaderDeclaredButAnEmptyExtent_Throws()
    {
      var space = Mixed(new object?[,] { { null, null }, { null, null } });

      var failure = Assert.Throws<ProjectionException>(() => Table(t => t.RowCount).Map(space));

      Assert.Contains("a header row was declared but the table's extent is empty", failure.Message);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(-1)]
    public void Table_WithMoreThanOneHeaderRow_IsRejectedAtConstruction(int headerRows)
    {
      var failure = Assert.Throws<ArgumentOutOfRangeException>(() => Table(headerRows, t => t.RowCount));

      Assert.Contains("multi-row headers are not supported in this release", failure.Message);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void TheRowLambda_WithMoreThanOneHeaderRow_IsRejectedAtConstruction(int headerRows)
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => Table(headerRows, r => r[0]));
    }

    // --- Tier 1: by index ------------------------------------------------------------------------------------

    [Fact]
    public void TableRow_IsAddressableByColumnIndex()
    {
      var values = Table(r => (r[0].GetString(), r[1].GetInt())).Map(SimpleTable());

      Assert.Equal(("Acme", 10), values[0]);
      Assert.Equal(("Gamma", 30), values[2]);
    }

    [Fact]
    public void TableRow_ExposesItsIndexCountAndCells()
    {
      var rows = Table(t => t.Rows).Map(SimpleTable());

      Assert.Equal(new[] { 0, 1, 2 }, rows.Select(r => r.Index).ToArray());
      Assert.All(rows, r => Assert.Equal(2, r.Count));
      Assert.Equal(new[] { "Acme", "10" }, rows[0].Cells.Select(c => c.TryGetString() ?? c.GetInt().ToString()).ToArray());
    }

    [Fact]
    public void TableRow_WithAnOutOfRangeIndex_ThrowsAndSaysHowManyColumnsThereAre()
    {
      var failure = Assert.Throws<ProjectionException>(() => Table(r => r[4].GetInt()).Map(SimpleTable()));

      Assert.Contains("column index 4 is out of range; the table has 2 columns.", failure.Message);
    }

    // --- Tier 2: by name -------------------------------------------------------------------------------------

    [Fact]
    public void TableRow_IsAddressableByColumnName()
    {
      var values = Table(r => (r["Investor"].GetString(), r["Amount"].GetInt())).Map(SimpleTable());

      Assert.Equal(("Acme", 10), values[0]);
      Assert.Equal(("Beta", 20), values[1]);
    }

    [Fact]
    public void ColumnNameLookup_IgnoresCaseAndSurroundingWhitespace()
    {
      var space = Mixed(new object?[,]
      {
        { "  Investor  ", "AMOUNT" },
        { "Acme", 10 },
      });

      var values = Table(r => (r["investor"].GetString(), r["amount"].GetInt())).Map(space);

      Assert.Equal(("Acme", 10), values[0]);
    }

    [Fact]
    public void UnknownColumnName_ThrowsAndListsTheColumnsThatExist()
    {
      var failure = Assert.Throws<ProjectionException>(() => Table(r => r["Net"].GetInt()).Map(SimpleTable()));

      Assert.Contains("there is no column named 'Net'; available columns: 'Investor', 'Amount'.", failure.Message);
    }

    [Fact]
    public void AWhitespaceOnlyHeaderCell_NamesNoColumn()
    {
      // The cell carries a value, so it is part of the table; it just does not name anything.
      var space = Mixed(new object?[,]
      {
        { "Investor", "   " },
        { "Acme", 10 },
      });

      var view = Table((TableView t) => t).Map(space);

      Assert.Equal(2, view.ColumnCount);
      Assert.Equal(new[] { "Investor", "" }, view.ColumnNames);
    }

    [Fact]
    public void ColumnNameLookup_TrimsTheKeyAsWellAsTheHeader()
    {
      // Names are matched trimmed at both ends of the comparison, so neither the sheet nor the
      // declaration has to be tidy about padding.
      var space = Mixed(new object?[,]
      {
        { " Investor ", "Amount" },
        { "Acme", 10 },
      });

      var values = Table(r => r["  Investor  "].GetString()).Map(space);

      Assert.Equal(new[] { "Acme" }, values);
    }

    [Fact]
    public void HeaderNamesDifferingOnlyByCase_AreAmbiguous()
    {
      // Lookup is case-insensitive, so two headers that differ only by case are the same column
      // name twice — and neither can be resolved.
      var space = Mixed(new object?[,]
      {
        { "Amount", "AMOUNT" },
        { 1, 2 },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table(r => r["Amount"].GetInt()).Map(space));

      Assert.Contains("appears at indices 0 and 1", failure.Message);
    }

    [Fact]
    public void DuplicateColumnName_ThrowsAndNamesTheIndices()
    {
      var space = Mixed(new object?[,]
      {
        { "Amount", "Investor", "Amount" },
        { 1, "Acme", 3 },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table(r => r["Amount"].GetInt()).Map(space));

      Assert.Contains("column 'Amount' appears at indices 0 and 2; use the index.", failure.Message);
    }

    [Fact]
    public void ColumnNameLookup_WithoutAHeaderRow_SaysToUseIndices()
    {
      var space = Mixed(new object?[,] { { "Acme", 10 } });

      var failure = Assert.Throws<ProjectionException>(() => Table(0, r => r["Investor"].GetString()).Map(space));

      Assert.Contains("the table was declared without a header row; use column indices.", failure.Message);
    }

    [Fact]
    public void BlankHeaderCells_AreNotAddressableByName()
    {
      var space = Mixed(new object?[,]
      {
        { "Investor", null },
        { "Acme", 10 },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table(r => r[""].GetInt()).Map(space));

      Assert.Contains("there is no column named ''; available columns: 'Investor'.", failure.Message);
    }

    // --- TryGet ------------------------------------------------------------------------------------------------

    [Fact]
    public void TryGet_FindsAKnownColumn()
    {
      var found = Table(r => r.TryGet("Amount", out var value) ? value.GetInt() : -1).Map(SimpleTable());

      Assert.Equal(new[] { 10, 20, 30 }, found);
    }

    [Fact]
    public void TryGet_ReturnsFalseForAnUnknownColumn()
    {
      var results = Table(r => r.TryGet("Net", out var value) ? "found" : value.IsBlank ? "missing" : "?").Map(SimpleTable());

      Assert.All(results, r => Assert.Equal("missing", r));
    }

    [Fact]
    public void TryGet_OnAHeaderlessTable_Throws()
    {
      // TryGet answers "is this optional column present?". Without a header row no name can mean
      // anything, so the question itself is broken — the same judgement the indexer makes, rather
      // than reporting every column as absent.
      var space = Mixed(new object?[,] { { "Acme", 10 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        Table(0, r => r.TryGet("Investor", out _) ? 1 : 0).Map(space));

      Assert.Contains("the table was declared without a header row; use column indices.", failure.Message);
    }

    [Fact]
    public void TryGet_ThrowsForAnAmbiguousColumn()
    {
      // An absent column is a question with an answer; an ambiguous one is a broken declaration.
      var space = Mixed(new object?[,]
      {
        { "Amount", "Amount" },
        { 1, 2 },
      });

      var failure = Assert.Throws<ProjectionException>(() =>
        Table(r => r.TryGet("Amount", out _) ? 1 : 0).Map(space));

      Assert.Contains("appears at indices 0 and 1; use the index.", failure.Message);
    }

    // --- Explicit placement --------------------------------------------------------------------------------------

    [Fact]
    public void Table_WithAnExplicitArea_UsesItInsteadOfDiscovering()
    {
      var applied = Table(t => (t.ColumnCount, t.RowCount))
        .Sized(AreaStrategies.ExplicitArea(1, 2))
        .Apply(SimpleTable());

      Assert.Equal((1, 1), applied.Value);
      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void Table_CanBeRepositioned()
    {
      // Down(1) replaces the default skip-blank-rows offset, so the table starts a row lower and
      // the first data row becomes its header — non-text header cells naming themselves "".
      var names = Table(t => t.ColumnNames).Down(1).Map(SimpleTable());

      Assert.Equal(new[] { "Acme", "" }, names);
    }

    // --- Table -------------------------------------------------------------------------------------------------

    [Fact]
    public void TheRowLambda_IsTheViewLambdaProjectedOverTheBodyRows()
    {
      var viaViewLambda = Table(t => t.Rows.Select(r => r["Amount"].GetInt()).ToList()).Map(SimpleTable());
      var viaRowLambda = Table(r => r["Amount"].GetInt()).Map(SimpleTable());

      Assert.Equal(viaViewLambda, viaRowLambda);
      Assert.Equal(new[] { 10, 20, 30 }, viaRowLambda);
    }

    [Fact]
    public void TheRowLambda_WithoutAHeader_ProjectsEveryRow()
    {
      var values = Table(0, r => r[0].TryGetString() ?? "-").Map(SimpleTable());

      Assert.Equal(new[] { "Investor", "Acme", "Beta", "Gamma" }, values);
    }

    [Fact]
    public void BothLambdaRungs_RejectANullProjection()
    {
      // Cast in both, because Table<T> carries a binding overload and two lambda rungs, and an
      // untyped null is convertible to all three delegate types. A lambda still resolves without
      // help unless its body touches nothing distinctive — see the note at the top of this file.
      Assert.Throws<ArgumentNullException>(() => Table((Func<TableView, int>)null!));
      Assert.Throws<ArgumentNullException>(() => Table((Func<TableRow, int>)null!));
    }
  }
}
