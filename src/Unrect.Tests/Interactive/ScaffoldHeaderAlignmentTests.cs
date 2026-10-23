using System;

using Unrect.Fluent;
using Unrect.Interactive;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// With bands, the row the label search settles on may be the header's top or its captions: a
  /// band row is often one merged cell over many captions, which the search steps over as too
  /// empty. The captions are decided by what lies around the found row, and the bands are read
  /// from the rows above them, so what is scaffolded is what <c>Table&lt;T&gt;(headerRows)</c>
  /// binds.
  /// </summary>
  public class ScaffoldHeaderAlignmentTests
  {
    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    /// <summary>What the scaffold writes: under a band a member is named by its path run together, as the table matches it.</summary>
    public sealed record Line(string MainName, decimal MainAmount, string MainCode);

    /// <summary>What a hand also writes, since a unique caption is a name on its own.</summary>
    public sealed record Bare(string Name, decimal Amount, string Code);

    /// <summary>One merged band over every caption: the band row is a single cell.</summary>
    private static ICellSpace Merged() => SheetGrid.Of(new object?[,]
    {
      { "Main", null, null },
      { "Name", "Amount", "Code" },
      { "a", 1.5m, "x" },
      { "b", 2.5m, "y" },
    });

    /// <summary>A title over a band row over the captions: the search steps over the title.</summary>
    private static ICellSpace Titled() => SheetGrid.Of(new object?[,]
    {
      { "Report", null, null, null },
      { "Date", "From", null, null },
      { null, "Id", "Net (USD)", "Notes" },
      { new DateTime(2026, 1, 5), 1m, 2.5m, null },
    });

    [Fact]
    public void AMergedBandRowIsReadAboveTheCaptionsTheSearchFound()
    {
      Assert.Equal(
        Lines(
          "// binds with Table<Line>(2): a member answers to a caption, or to its band and caption run together",
          "public sealed record Line(string MainName, decimal MainAmount, string MainCode);"),
        Merged().ScaffoldRecord("Line", headerRows: 2));
    }

    [Fact]
    public void ATitleOverABandRowIsSteppedOverAndTheBandsReadBelowIt()
    {
      Assert.Equal(
        Lines(
          "// binds with Table<Row>(2): a member answers to a caption, or to its band and caption run together",
          "// \"Net (USD)\" does not bind to FromNetUSD by name: .Column(r => r.FromNetUSD, \"From\", \"Net (USD)\")",
          "public sealed record Row(DateTime Date, int FromId, decimal FromNetUSD, string? FromNotes);"),
        Titled().ScaffoldRecord("Row", headerRows: 2));
    }

    [Fact]
    public void AtNamesTheRowOfCaptionsAndTheBandsMustFitAboveIt()
    {
      Assert.Equal(
        Lines(
          "// binds with Table<Line>(2): a member answers to a caption, or to its band and caption run together",
          "public sealed record Line(string MainName, decimal MainAmount, string MainCode);"),
        Merged().ScaffoldRecord("Line", at: 1, headerRows: 2));

      Assert.Equal("at", Assert.Throws<ArgumentOutOfRangeException>(() => Merged().ScaffoldRecord("Line", at: 0, headerRows: 2)).ParamName);
    }

    [Fact]
    public void TheRecordScaffoldedIsTheOneTheTableBinds()
    {
      Assert.Equal(
        "public sealed record Line(string MainName, decimal MainAmount, string MainCode);",
        Merged().ScaffoldRecord("Line", headerRows: 2).Split(new[] { Environment.NewLine }, StringSplitOptions.None)[1]);

      Assert.Equal(
        new[] { new Line("a", 1.5m, "x"), new Line("b", 2.5m, "y") },
        Table<Line>(2).Map(Merged()));

      // The band is one cell over everything, so the bare captions name the columns too.
      Assert.Equal(
        new[] { new Bare("a", 1.5m, "x"), new Bare("b", 2.5m, "y") },
        Table<Bare>(2).Map(Merged()));
    }
  }
}
