using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>Table()</c> with no projection and no type: each row as a dictionary keyed by the
  /// header's captions. It is the exploratory spelling — what a script reaches for before the
  /// record exists — and its one promise is that it reads the sheet without deciding anything.
  /// <para>
  /// Nothing is stringified. A date column stays <c>Temporal</c>, a blank stays <c>Blank</c>, an
  /// error stays <c>Error</c>; interpreting them is the caller's job, at the point where the
  /// meaning is known.
  /// </para>
  /// </summary>
  public class DictionaryTableTests
  {
    private static ICellSpace Sheet() => Mixed(new object?[,]
    {
      { "Investor Name", "Transaction Date", "Amount" },
      { "Acme", new DateTime(2026, 3, 4), 10m },
      { "Beta", null, 20m },
    });

    private static IReadOnlyList<IReadOnlyDictionary<string, Point<ICellSpace>>> Rows() => Table().Map(Sheet());

    // --- Keys and values ---------------------------------------------------------------------------

    [Fact]
    public void TheKeysAreTheHeadersCaptions()
    {
      Assert.Equal(new[] { "Investor Name", "Transaction Date", "Amount" }, Rows()[0].Keys.ToArray());
    }

    [Fact]
    public void NothingIsStringified()
    {
      var rows = Rows();

      Assert.Equal("Acme", rows[0]["Investor Name"].Text());
      Assert.Equal(new DateTime(2026, 3, 4), rows[0]["Transaction Date"].Date());
      Assert.Equal(10m, rows[0]["Amount"].Decimal());

      // A blank cell is a blank cell rather than an absent key or an empty string.
      Assert.True(rows[1]["Transaction Date"].IsBlank());
      Assert.Null(rows[1]["Transaction Date"].AsText());
      Assert.True(rows[1].ContainsKey("Transaction Date"));
    }

    [Fact]
    public void AnErrorCellSurvivesAsAnError()
    {
      var space = Mixed(new object?[,]
      {
        { "Amount" },
        { CellValue.OfError(CellError.DivisionByZero) },
      });

      var cell = Table().Map(space)[0]["Amount"];

      Assert.True(cell.IsError());
      Assert.Equal("#DIV/0!", cell.AsText());
      Assert.Equal("Error(#DIV/0!)", cell.Describe());
    }

    [Fact]
    public void LookupsGoThroughTheCaptionComparer()
    {
      // The exploratory spelling should not make a reader retype a caption exactly.
      var row = Rows()[0];

      Assert.Equal("Acme", row["investorname"].Text());
      Assert.Equal("Acme", row["  Investor  Name  "].Text());
      Assert.Equal(10m, row["amount"].Decimal());
    }

    [Fact]
    public void TheElementTypeIsADictionaryOfPoints()
    {
      // The rung's declared element type, pinned rather than inferred from a var: one
      // IReadOnlyDictionary<string, Point<TSpace>> per body row, and the values really are
      // ADDRESSES rather than anything that renders like a value — so what a caller can ask of one
      // is whatever their own space answers. The static side of this assertion is the local's type;
      // the runtime side is the closed interface the factory's projection implements, so the pin
      // holds even if the factory is later composed out of other projections.
      IProjectionDefinition<ICellSpace, IReadOnlyList<IReadOnlyDictionary<string, Point<ICellSpace>>>> table = Table();

      Assert.Contains(
        typeof(IProjectionDefinition<ICellSpace, IReadOnlyList<IReadOnlyDictionary<string, Point<ICellSpace>>>>),
        table.GetType().GetInterfaces());

      IReadOnlyDictionary<string, Point<ICellSpace>> row = table.Map(Sheet())[0];
      object value = row["Amount"];

      Assert.IsType<Point<ICellSpace>>(value);
    }

    [Fact]
    public void TheDictionaryIsReadOnlyAndCarriesTheComparer()
    {
      var row = Rows()[0];

      Assert.IsAssignableFrom<IReadOnlyDictionary<string, Point<ICellSpace>>>(row);
      Assert.True(row.ContainsKey("TRANSACTIONDATE"));
      Assert.False(row.ContainsKey("Nope"));
    }

    // --- Failures ------------------------------------------------------------------------------------------

    [Fact]
    public void TwoCaptionsThatCollideUnderTheComparer_AreALoudFailure()
    {
      // Textually different, the same name to the comparer — so a lookup would be a coin toss.
      var space = Mixed(new object?[,]
      {
        { "Net Amount", "NetAmount" },
        { 1m, 2m },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table().Map(space));

      Assert.Contains(
        "the columns at A1 ('Net Amount') and B1 ('NetAmount') carry the same caption; "
        + "captions are matched ignoring case and whitespace",
        failure.Message);
    }

    [Fact]
    public void AColumnWithNoCaption_IsALoudFailure()
    {
      var space = Mixed(new object?[,]
      {
        { "Amount", null },
        { 1m, 2m },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table().Map(space));

      Assert.Contains(
        "the column at B1 has no caption and holds values; every column needs a caption to be read by name",
        failure.Message);
    }

    [Fact]
    public void AColumnWithNoCaptionAndNothingInIt_HasNoEntry()
    {
      // The blank lead of an indented table: part of the table's extent, with no caption to be
      // read by and nothing to lose by not reading it. Sight-reading an unfamiliar sheet should not
      // fail on the margin it was laid out with.
      var space = Mixed(new object?[,]
      {
        { null, "Name", "Amount" },
        { null, "Acme", 1m },
      });

      var row = Assert.Single(Table().Map(space));

      Assert.Equal(new[] { "Name", "Amount" }, row.Keys);
    }

    [Fact]
    public void AHeaderDeclaredOverAnEmptyExtent_IsALoudFailure()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Table().Map(Mixed(new object?[,] { { null, null }, { null, null } })));

      Assert.Contains("a header row was declared but the table's extent is empty", failure.Message);
    }

    // --- Defaults are the table's --------------------------------------------------------------------------

    [Fact]
    public void TheDefaultsAreTheOnesEveryTableHas()
    {
      // Leading blank rows skipped, extent discovered — the same placement the projecting spelling
      // gets, because it is the same shape underneath.
      var space = Mixed(new object?[,]
      {
        { null, null },
        { "Investor Name", "Amount" },
        { "Acme", 10m },
        { null, null },
        { "not part of the table", null },
      });

      var applied = Table().Apply(space);

      Assert.Single(applied.Value);
      Assert.Equal("Acme", applied.Value[0]["Investor Name"].Text());
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void AHeaderWithNoBodyYieldsNoRows()
    {
      Assert.Empty(Table().Map(Mixed(new object?[,] { { "Investor Name", "Amount" } })));
    }
  }
}
