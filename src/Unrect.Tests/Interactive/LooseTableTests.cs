using System;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Interactive.ExploratoryBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// The forgiving table: <c>Table&lt;T&gt;</c> with its one strictness switched, for a type that is
  /// still being written. The case it exists for is a property added to a record an hour before the
  /// code that fills it — which a strict table refuses, rightly, and an exploring one should not.
  /// </summary>
  public class LooseTableTests
  {
    public sealed record Fund(string Code, decimal Nav, bool IsDeprecated, string? Manager);

    private static ISheetCells Sheet() => SheetGrid.Of(new object?[,]
    {
      { "Code", "NAV", "Region", "Inception" },
      { "A-1", 10m, "EU", new DateTime(2020, 1, 1) },
      { "B-2", 20m, "US", new DateTime(2021, 1, 1) },
    });

    [Fact]
    public void AMemberNoColumnBindsIsLeftAtItsDefault()
    {
      // Strict, this is the failure that stops the script; loose, the rows still come back, with
      // the value type at its default and the nullable at null.
      Assert.Contains("no column binds Fund.IsDeprecated or Fund.Manager",
        Assert.Throws<ProjectionException>(() => Table<Fund>().Map(Sheet())).Message, StringComparison.Ordinal);

      var funds = LooseTable<Fund>().Map(Sheet());

      Assert.Equal(new[] { new Fund("A-1", 10m, false, null), new Fund("B-2", 20m, false, null) }, funds);
    }

    [Fact]
    public void AndItSaysSoOnceWithTheColumnsNobodyReads()
    {
      // The two things a half-written type wants to be told, each said once for the table and not
      // once per row: what it asked for that is not there, and what is there that it did not ask for.
      var read = LooseTable<Fund>().MapWithDiagnostics(Sheet());

      var warning = Assert.Single(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      var info = Assert.Single(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);

      Assert.Equal(
        "no column binds Fund.IsDeprecated or Fund.Manager, left at their defaults; "
        + "the table's captions are 'Code', 'NAV', 'Region', 'Inception'",
        warning.Message);
      Assert.Equal("no member of Fund reads the columns 'Region', 'Inception'", info.Message);
    }

    [Fact]
    public void ATypeThatMatchesItsTableSaysNothing()
    {
      var sheet = SheetGrid.Of(new object?[,] { { "Code", "NAV", "IsDeprecated", "Manager" }, { "A-1", 10m, true, "Ann" } });

      var read = LooseTable<Fund>().MapWithDiagnostics(sheet);

      Assert.Equal(new Fund("A-1", 10m, true, "Ann"), Assert.Single(read.Value));
      Assert.Empty(read.Diagnostics);
    }

    [Fact]
    public void EverythingElseIsAsLoudAsItAlwaysWas()
    {
      // Loose about a type still being written; not about the file. A wrong kind is a fact about
      // the file, and a duplicated caption is a table nobody can read by name.
      var wrongKind = SheetGrid.Of(new object?[,] { { "Code", "NAV" }, { "A-1", "n/a" } });
      var duplicated = SheetGrid.Of(new object?[,] { { "Code", "NAV", "nav" }, { "A-1", 1m, 2m } });

      var kind = Assert.Throws<ProjectionException>(() => LooseTable<Fund>().Map(wrongKind));

      Assert.Equal("Table<Fund>[0] -> column 'Nav'", kind.Path);
      Assert.Contains("expected Number at B2, found Text", kind.Message, StringComparison.Ordinal);
      Assert.Contains("Fund.Nav matches the columns at B1", Assert.Throws<ProjectionException>(() => LooseTable<Fund>().Map(duplicated)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheBindingsAreTheStrictTablesOwn()
    {
      // The same builder with one decision switched: a caption override, a position and a row
      // reading all work, and a member they fill is not among the ones reported.
      var read = LooseTable<Fund>(bind => bind
        .Column(f => f.Manager, "Region")
        .Column(f => f.IsDeprecated, row => row["Code"].Text().StartsWith("B", StringComparison.Ordinal)))
        .MapWithDiagnostics(Sheet());

      Assert.Equal(new[] { new Fund("A-1", 10m, false, "EU"), new Fund("B-2", 20m, true, "US") }, read.Value);
      Assert.DoesNotContain(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Equal("no member of Fund reads the column 'Inception'", Assert.Single(read.Diagnostics).Message);
    }
  }
}
