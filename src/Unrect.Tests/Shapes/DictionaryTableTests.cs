using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// <c>TableRows()</c> with no projection and no type: each row as a dictionary keyed by the
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
    private static ISpace Table() => Mixed(new object?[,]
    {
      { "Investor Name", "Transaction Date", "Amount" },
      { "Acme", new DateTime(2026, 3, 4), 10m },
      { "Beta", null, 20m },
    });

    private static IReadOnlyList<IReadOnlyDictionary<string, CellValue>> Rows() => TableRows().Map(Table());

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

      Assert.Equal(CellKind.Text, rows[0]["Investor Name"].Kind);
      Assert.Equal(CellKind.Temporal, rows[0]["Transaction Date"].Kind);
      Assert.Equal(CellKind.Number, rows[0]["Amount"].Kind);

      // A blank cell is a blank value rather than an absent key or an empty string.
      Assert.Equal(CellKind.Blank, rows[1]["Transaction Date"].Kind);
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

      var cell = TableRows().Map(space)[0]["Amount"];

      Assert.Equal(CellKind.Error, cell.Kind);
      Assert.Equal(CellError.DivisionByZero, cell.GetError());
    }

    [Fact]
    public void LookupsGoThroughTheCaptionComparer()
    {
      // The exploratory spelling should not make a reader retype a caption exactly.
      var row = Rows()[0];

      Assert.Equal("Acme", row["investorname"].GetString());
      Assert.Equal("Acme", row["  Investor  Name  "].GetString());
      Assert.Equal(10m, row["amount"].GetDecimal());
    }

    [Fact]
    public void TheDictionaryIsReadOnlyAndCarriesTheComparer()
    {
      var row = Rows()[0];

      Assert.IsAssignableFrom<IReadOnlyDictionary<string, CellValue>>(row);
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

      var failure = Assert.Throws<ShapeException>(() => TableRows().Map(space));

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

      var failure = Assert.Throws<ShapeException>(() => TableRows().Map(space));

      Assert.Contains(
        "the column at B1 has no caption; every column needs one to be read by name",
        failure.Message);
    }

    [Fact]
    public void AHeaderDeclaredOverAnEmptyExtent_IsALoudFailure()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        TableRows().Map(Mixed(new object?[,] { { null, null }, { null, null } })));

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

      var applied = TableRows().Apply(space);

      Assert.Single(applied.Value);
      Assert.Equal("Acme", applied.Value[0]["Investor Name"].GetString());
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void AHeaderWithNoBodyYieldsNoRows()
    {
      Assert.Empty(TableRows().Map(Mixed(new object?[,] { { "Investor Name", "Amount" } })));
    }
  }
}
