using System;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Interactive.ScaffoldBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// The scaffolds as leaves: a type guessed where a declaration has already found it.
  /// <para>
  /// What the source says is <see cref="ScaffoldingTests"/>' business. What is pinned here is WHERE a
  /// scaffold stands — that it takes the region the table it stands in for will take, so that
  /// replacing one with the other moves nothing else in the declaration.
  /// </para>
  /// </summary>
  public class ScaffoldBuildersTests
  {
    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    /// <summary>A title, a gap, a table, a gap, and a second table of a different shape.</summary>
    private static ICellSpace Report()
      => SheetGrid.Of(new object?[,]
      {
        { "Fund Report", null, null },
        { null, null, null },
        { "Structure", "Share", null },
        { "Feeder", 0.25m, null },
        { "Master", 0.75m, null },
        { null, null, null },
        { "Client", "Trade Date", "Amount" },
        { "Acme", new DateTime(2026, 1, 5), 100m },
        { "Bolt", new DateTime(2026, 1, 6), 250m },
      });

    public sealed record FundStructure(string Structure, decimal Share);

    public sealed record Transaction(string Client, DateTime TradeDate, int Amount);

    [Fact]
    public void ScaffoldsAreWrittenWhereTheDeclarationFoundThem()
    {
      // The owner's sketch. Nothing is searched for: the flow puts each scaffold on its table, past
      // the title and the gaps, and each reads the labels of the region it was handed.
      var report = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        FundStructures = v.Next(ScaffoldClass("FundStructure")),
        Transactions = v.Next(ScaffoldRecord("Transaction")),
      });

      var read = report.Map(Report());

      Assert.Equal("Fund Report", read.Title);
      Assert.Equal(
        Lines(
          "public sealed class FundStructure",
          "{",
          "    public string Structure { get; init; } = \"\";",
          "    public decimal Share { get; init; }",
          "}"),
        read.FundStructures);
      Assert.Equal("public sealed record Transaction(string Client, DateTime TradeDate, int Amount);", read.Transactions);
    }

    [Fact]
    public void AScaffoldStandsWhereTheTableWill()
    {
      // The point of the leaf form: the types above are the two scaffolds' output, pasted, and the
      // declaration below is the one above with each scaffold replaced by the table it stood in for.
      // That it reads — the second table found, nothing left over between them — is the proof that a
      // scaffold consumed exactly what Table<T>() consumes.
      var report = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        FundStructures = v.Next(Table<FundStructure>()),
        Transactions = v.Next(Table<Transaction>()),
      });

      var read = report.Map(Report());

      Assert.Equal(new[] { new FundStructure("Feeder", 0.25m), new FundStructure("Master", 0.75m) }, read.FundStructures);
      Assert.Equal(new[] { 100, 250 }, read.Transactions.Select(t => t.Amount));
    }

    [Fact]
    public void ACardIsScaffoldedFromTheRowsThatCarryIt()
    {
      // Labels down the first column: the leaf takes the full width for as many rows as hold a value,
      // so the table under the gap is not part of the card and is still there for what follows.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { null, null },
        { "Fund Name", "Alpha" },
        { "Partners", 12m },
        { null, null },
        { "Client", "Amount" },
        { "Acme", 100m },
      });

      var report = VerticalFlow(v => new
      {
        Fund = v.Next(ScaffoldRecord("Fund", Unrect.Interactive.LabelsIn.Column)),
        Transactions = v.Next(ScaffoldRecord("Transaction")),
      });

      var read = report.Map(sheet);

      Assert.Equal("public sealed record Fund(string FundName, int Partners);", read.Fund);
      Assert.Equal("public sealed record Transaction(string Client, int Amount);", read.Transactions);
    }

    [Fact]
    public void InsideARepeatAScaffoldSaysItsPieceOncePerOccurrence()
    {
      // Not a use anyone should reach for — scaffold the item, then repeat the item — but it is what
      // was asked for, so it is what comes back: one guess per block, each from its own samples.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Amount" },
        { 1m },
        { null },
        { "Amount" },
        { 2.5m },
      });

      var guesses = VerticalRepeat(ScaffoldRecord("Row"), separatedBy: BlankRows()).Map(sheet);

      Assert.Equal(
        new[] { "public sealed record Row(int Amount);", "public sealed record Row(decimal Amount);" },
        guesses);
    }

    [Fact]
    public void AScaffoldIsRefusedWhereItIsWritten()
    {
      // Declaring is side-effect free and eager about its own arguments, like every other factory.
      Assert.Equal("typeName", Assert.Throws<ArgumentNullException>(() => ScaffoldRecord(null!)).ParamName);
      Assert.Equal("typeName", Assert.Throws<ArgumentException>(() => ScaffoldClass(" ")).ParamName);
      Assert.Equal("samples", Assert.Throws<ArgumentOutOfRangeException>(() => ScaffoldRecord("Row", samples: -1)).ParamName);
    }
  }
}
