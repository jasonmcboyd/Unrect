using PlacementGauntlet.Staged;

using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario 4 — hoisted reuse. One section declared once and placed twice, through the
  /// terminal that takes an existing projection: <c>On(mark).Of(section)</c>, which is alternative
  /// B's <c>Placed(On(mark), section)</c> in fluent clothes (§5.2's convergence, spelled).
  /// <para>
  /// The reuse rule the vocabulary already has — a helper must not place or name what it returns,
  /// because the use site is the only place that knows which occurrence this is — comes out of the
  /// inversion looking like a grammar rather than a convention: an unplaced projection is the only
  /// thing <c>.Of</c> accepts without the shipped runtime refusal firing.
  /// </para>
  /// </summary>
  public static class Scenario4
  {
    public static void Run()
    {
      Judge.Section("Scenario 4 — hoisted reuse, placed twice");

      var sheet = Sheets.Regions();

      var lines = Table<Line>();

      var section = VerticalFlow(v => new Region(
        Name: v.Next(Text()),
        Lines: v.Next(lines)));

      // OLD — the placement is a postfix on the reused value.
      var oldReport = VerticalFlow(v => new
      {
        A = v.Next(section.On(RowContaining("Region A"))),
        B = v.Next(section.On(RowContaining("Region B"))),
      });

      // NEW — the placement leads and the reused value terminates it.
      var newReport = VerticalFlow(v => new
      {
        A = v.Next(On(RowContaining("Region A")).Of(section)),
        B = v.Next(On(RowContaining("Region B")).Of(section)),
      });

      Judge.Same("a section placed twice reads the same either way", oldReport.Map(sheet), newReport.Map(sheet));

      // The same, bounded at the use site — the investor-irr pattern, which is the reason .Until had
      // to stay composable after a projection as well as being a stage.
      var oldBounded = section.On(RowContaining("Region A")).Until(RowContaining("Region B"));
      var newBounded = On(RowContaining("Region A")).Until(RowContaining("Region B")).Of(section);

      Judge.Same("bounded at the use site: stage order and postfix order agree",
        oldBounded.Map(sheet), newBounded.Map(sheet));
      Judge.Note("SUPERSEDED by the geography law: the bound stage is dropped, and the same declaration reads"
        + " On(a).Of(section).Until(b) — anchor before the subject because a landmark above it locates it, bound"
        + " after because the landmark that ends it is below. Kept here as the first trial's record.");

      // A placement declared at the use site over a HOISTED projection that already declares one is
      // still refused — by the shipped runtime guard, because a terminal hands back a plain
      // projection and the pipeline's types end there. This is the intended hybrid of §5.0.
      Judge.Refused("re-placing an already-placed section is still a construction-time refusal",
        () => On(RowContaining("Region B")).Of(section.On(RowContaining("Region A"))));

      ScopedPlacement();
    }

    // --- One placement under a scope ----------------------------------------------------------------

    private static void ScopedPlacement()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(Sheets.Example("investors-by-deal.xlsx"), "Investors");

      var deal = VerticalFlow(v => new
      {
        DealCode = v.Next(Text()),
        Transactions = v.Next(Table<DealTransaction>()),
      });

      var oldDeals = VerticalRepeat(deal, separatedBy: BlankRows());

      // The hoisted deal block is plain and stays plain; the use site raises the declaration's demand
      // by placing it through a scope. Nothing about `deal` changes, which is the property the
      // weakest-demand guidance asks for.
      var newDeals = Place.Over<ISpreadsheetSpace>().Offset().VerticalRepeat(deal, separatedBy: BlankRows());

      Judge.Same("a scoped placement raises the demand and reads identically",
        oldDeals.Map(sheet), newDeals.Map(sheet));
      Judge.Note("The scope is at the USE site, so the reusable declaration never acquires a demand it does not read —"
        + " the pipeline makes 'demand the weakest thing that works' the path of least resistance.");
    }
  }
}
