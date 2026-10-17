using Unrect.Projections;
using Unrect.Fluent;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <see cref="ProjectionBuilders{TSpace}"/> — the file-scoped vocabulary, and now the only one.
  /// <para>
  /// This suite used to compare the closed class against a second, open <c>Projection</c> family it
  /// re-exported, member for member. That family is gone: there is one vocabulary, so there is
  /// nothing left to be at parity <em>with</em>, and the twin tables, the completeness covenant and
  /// the leaf-stays-plain boundary went with it. What survives is what was never about the pairing:
  /// </para>
  /// <para>
  /// <b>Name capture, per site.</b> The members that forward a <c>CallerArgumentExpression</c> are
  /// the one defect class a one-line forwarder can hide completely — drop the <c>declared</c>
  /// argument and the inner factory captures the text at the FORWARDING site, which reads like a
  /// correct capture wherever the caller's identifier happens to match the forwarder's parameter
  /// name. No identifier below is called <c>item</c> or <c>eachRow</c>, for exactly that reason.
  /// </para>
  /// <para>
  /// The disjointness laws that stood here — the core class against the two backend classes a file
  /// once imported beside it — went with those classes: one vocabulary has nothing to be disjoint
  /// from.
  /// </para>
  /// </summary>
  public class ProjectionBuildersParityTests
  {
    private static ICellSpace Ledger() => Mixed(new object?[,]
    {
      { "Fund", "Amount", "Units" },
      { "Alpha", 100m, 2 },
      { "Beta", 250m, 4 },
    });

    /// <summary>The same bind pointed at the column of fund names, so every record fails.</summary>
    private static IProjectionDefinition<ICellSpace, decimal> FundColumnAsANumber(LabelMap captions) => Right(captions["Fund"]).Of(Point().Decimal());

    // --- 1. Name capture, one pin per forwarding site ----------------------------------------------

    [Fact]
    public void AVerticalRepeatsItemKeepsTheIdentifierItWasWrittenAs()
    {
      var investorDetail = Point().Decimal();

      var failure = Assert.Throws<ProjectionException>(() => VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("'investorDetail'", failure.Subject);
      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndSoDoesAHorizontalRepeatsItem()
    {
      var quarterlyColumn = Point().Decimal();

      var failure = Assert.Throws<ProjectionException>(() => HorizontalRepeat(quarterlyColumn).Map(Ledger()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndATablesRowSlot()
    {
      var allocationRow = Point().Decimal();

      var failure = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndATablesBind_LabelledByTheMethodGroupItWasPassedAs()
    {
      var failure = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Ledger()));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndAnExplicitNameStillOutranksTheIdentifier()
    {
      var investorDetail = Point().Decimal().Named("detail");

      var failure = Assert.Throws<ProjectionException>(() => VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("VerticalRepeat[0] -> 'detail' (Decimal)", failure.Path);
    }
  }
}
