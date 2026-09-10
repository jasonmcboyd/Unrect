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

      // OLD — the library's shipped pipeline (the retired postfix on the reused value is gone).
      var oldReport = VerticalFlow(v => new
      {
        A = v.Next(Projection.On(RowContaining("Region A")).Of(section)),
        B = v.Next(Projection.On(RowContaining("Region B")).Of(section)),
      });

      // NEW — the placement leads and the reused value terminates it.
      var newReport = VerticalFlow(v => new
      {
        A = v.Next(Place.On(RowContaining("Region A")).Of(section)),
        B = v.Next(Place.On(RowContaining("Region B")).Of(section)),
      });

      Judge.Same("a section placed twice reads the same either way", oldReport.Map(sheet), newReport.Map(sheet));

      // The same, bounded at the use site — the investor-irr pattern, which is the reason .Until had
      // to stay composable after a projection as well as being a stage.
      var oldBounded = Projection.On(RowContaining("Region A")).Until(RowContaining("Region B")).Of(section);
      var newBounded = Place.On(RowContaining("Region A")).Until(RowContaining("Region B")).Of(section);

      Judge.Same("bounded at the use site: stage order and postfix order agree",
        oldBounded.Map(sheet), newBounded.Map(sheet));
      Judge.Note("SUPERSEDED by the geography law: the bound stage is dropped, and the same declaration reads"
        + " On(a).Of(section).Until(b) — anchor before the subject because a landmark above it locates it, bound"
        + " after because the landmark that ends it is below. Kept here as the first trial's record.");

      // SUPERSEDED by phase 5: the declared-over-declared RUNTIME refusal was retired along with the
      // postfix modifiers. The pipeline makes double-placement unspellable through its stage TYPES
      // instead, so a second offset written mid-pipeline is a compile error — but placing an already
      // placed projection through `.Of` no longer throws at construction; it simply replaces the
      // offset. Kept as a note rather than a runtime assertion, because the premise (a runtime guard)
      // is gone.
      Judge.Note("re-placing an already-placed section is no longer a runtime refusal: phase 5 moved the"
        + " declared-over-declared discipline into the pipeline's stage types, where a second offset is a"
        + " compile error, and `.Of` over an already-placed projection now silently replaces the offset.");

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
