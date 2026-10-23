using System;

using Unrect.Core;
using Unrect.Fluent;
using Unrect.Interactive;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// A scaffold reads a header the way the table does: by position, <c>headerRows</c> rows from
  /// the header's top, the last of them the captions. <c>at</c> is where the table starts. When the
  /// top is found rather than named, the search settles on a row of labels and counts the rows
  /// above it as bands when they exist and hold text — a merged band row is one cell the search
  /// steps over as too empty — so what is scaffolded is what <c>Table&lt;T&gt;(headerRows)</c> binds.
  /// </summary>
  public class ScaffoldHeaderAlignmentTests
  {
    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    /// <summary>What the scaffold writes: under a band a member is named by its path run together, as the table matches it.</summary>
    public sealed record Line(string MainName, decimal MainAmount, string MainCode);

    /// <summary>What a hand also writes, since a unique caption is a name on its own.</summary>
    public sealed record Bare(string Name, decimal Amount, string Code);

    private const string MergedScaffold = "public sealed record Line(string MainName, decimal MainAmount, string MainCode);";

    private const string BindsNote = "// binds with Table<Line>(2): a member answers to a caption, or to its band and caption run together";

    /// <summary>One merged band over every caption: the band row is a single cell.</summary>
    private static ICellSpace Merged() => SheetGrid.Of(new object?[,]
    {
      { "Main", null, null },
      { "Name", "Amount", "Code" },
      { "a", 1.5m, "x" },
      { "b", 2.5m, "y" },
    });

    /// <summary>A title over a band row over the captions.</summary>
    private static ICellSpace Titled() => SheetGrid.Of(new object?[,]
    {
      { "Report", null, null, null },
      { "Date", "From", null, null },
      { null, "Id", "Net (USD)", "Notes" },
      { new DateTime(2026, 1, 5), 1m, 2.5m, null },
    });

    [Fact]
    public void AMergedBandRowTheSearchSteppedOverIsCountedAsTheHeadersTop()
    {
      Assert.Equal(Lines(BindsNote, MergedScaffold), Merged().ScaffoldRecord("Line", headerRows: 2));
    }

    [Fact]
    public void AtIsWhereTheTableStarts()
    {
      Assert.Equal(Lines(BindsNote, MergedScaffold), Merged().ScaffoldRecord("Line", at: 0, headerRows: 2));

      // Started on the captions, a two-row header reads the captions as a band and the first data
      // row as captions — exactly what Table<Line>(2) placed with Down(1) would do.
      Assert.Equal(
        Lines(
          BindsNote,
          "// \"1.5\" does not bind to Amount15 by name: .Column(l => l.Amount15, \"Amount\", \"1.5\")",
          "public sealed record Line(string NameA, decimal Amount15, string CodeX);"),
        Merged().ScaffoldRecord("Line", at: 1, headerRows: 2));
    }

    [Fact]
    public void TheHeaderRowsMustFitBelowTheTop()
    {
      var twoRows = SheetGrid.Of(new object?[,] { { "Main", null }, { "Name", "Amount" } });

      Assert.Throws<OutOfBoundsException>(() => twoRows.ScaffoldRecord("Line", at: 1, headerRows: 2));
      Assert.Throws<OutOfBoundsException>(() => twoRows.ScaffoldRecord("Line", at: 2, headerRows: 1));
    }

    [Fact]
    public void ATitleDirectlyAboveABandedHeaderCannotBeToldFromABandAndWantsAtSaid()
    {
      // The search finds the band row (two labels of four) and the title above it is a row of
      // text, so it is counted as the top — the documented limit, which at: resolves. Read that
      // way the title is a band over the two columns the band row captions, which is exactly what
      // Table<Row>(2) placed at the title would read.
      Assert.Equal(
        Lines(
          "// binds with Table<Row>(2): a member answers to a caption, or to its band and caption run together",
          "public sealed record Row(DateTime? ReportDate, string ReportFrom);"),
        Titled().ScaffoldRecord("Row", headerRows: 2));

      Assert.Equal(
        Lines(
          "// binds with Table<Row>(2): a member answers to a caption, or to its band and caption run together",
          "// \"Net (USD)\" does not bind to FromNetUSD by name: .Column(r => r.FromNetUSD, \"From\", \"Net (USD)\")",
          "public sealed record Row(DateTime Date, int FromId, decimal FromNetUSD, string? FromNotes);"),
        Titled().ScaffoldRecord("Row", at: 1, headerRows: 2));
    }

    [Fact]
    public void TheRecordScaffoldedIsTheOneTheTableBinds()
    {
      Assert.Equal(
        MergedScaffold,
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
