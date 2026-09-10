using System;

using Unrect.Projections;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario 5 — a K-1-style nested section: an outer region anchored by its caption, inner
  /// content anchored WITHIN it. This is the spelling the declared-over-declared refusal pushes
  /// every sequential anchoring into, so it is the one the inversion has to make readable.
  /// <para>
  /// The sheet is synthetic (<c>Sheets.K1()</c>): <c>examples/scrubbed-k1.xlsx</c> is local-only and
  /// must never be committed, so the corner case is distilled instead — two captioned sections, each
  /// carrying a captioned table, and a terminator the second is bounded by.
  /// </para>
  /// </summary>
  public static class Scenario5
  {
    public static void Run()
    {
      Judge.Section("Scenario 5 — a K-1-style nested section");

      var sheet = Sheets.K1();

      // The inner content: a table whose columns are found by THIS file's captions, so every leaf
      // inside is anchored within the section the outer pipeline placed. Nesting, not chaining.
      var kLines = Table(headerRows: 1, eachRow: captions => Overlay(o => new KLine(
        Code: o.Next(Right(captions["Line"]).Text()),
        Amount: o.Next(Right(captions["Amount"]).Decimal()))));

      var oldLines = Table(headerRows: 1, eachRow: captions => Overlay(o => new KLine(
        Code: o.Next(Text().Right(captions["Line"])),
        Amount: o.Next(Decimal().Right(captions["Amount"])))));

      var title = Text();

      // OLD — anchor and bound as postfix modifiers on the section.
      var oldK1Section = VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("K-1 Lines 1-21")),
          Lines: v.Next(oldLines)))
        .On(RowContaining("K-1 Lines 1-21"))
        .Until(RowContaining("Portfolio Income"));

      var oldPortfolio = VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("Portfolio Income")),
          Lines: v.Next(oldLines)))
        .On(RowContaining("Portfolio Income"))
        .Until(RowContaining("Totals"));

      var oldReport = VerticalFlow(v => new K1Report(
        Title: v.Next(title),
        Lines: v.Next(oldK1Section),
        Portfolio: v.Next(oldPortfolio)));

      // NEW — the section's placement leads, and the region it places is where the inner anchors look.
      var newK1Section = On(RowContaining("K-1 Lines 1-21")).Until(RowContaining("Portfolio Income"))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("K-1 Lines 1-21")),
          Lines: v.Next(kLines)));

      var newPortfolio = On(RowContaining("Portfolio Income")).Until(RowContaining("Totals"))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("Portfolio Income")),
          Lines: v.Next(kLines)));

      var newReport = VerticalFlow(v => new K1Report(
        Title: v.Next(title),
        Lines: v.Next(newK1Section),
        Portfolio: v.Next(newPortfolio)));

      Judge.Same("the nested sections read the same under both spellings", oldReport.Map(sheet), newReport.Map(sheet));
      Judge.Note("Read aloud, the new spelling is the document's own sentence: \"on 'K-1 Lines 1-21', until"
        + " 'Portfolio Income': a caption over a table of lines\".");
      Judge.Note("The bound stage here is the FIRST TRIAL's spelling; under the geography law the same section reads"
        + " On(mark).VerticalFlow(v => …).Until(landmark), verified as acceptance read 3 in ScenarioG.");

      FrameHazard(sheet);
    }

    // --- What the fixed stage order buys ------------------------------------------------------------
    //
    // Hazard 4 of the congruence survey: an extent declared OUTSIDE a bound becomes the frame the
    // landmark is sought in, so a working declaration turns into "the landmark does not exist". It is
    // meaningful semantics, not a contradiction — which is why it was never a candidate for the
    // runtime refusal. The pipeline dissolves it by GRAMMAR: after a bound, only a terminal follows.

    private static void FrameHazard(Unrect.Core.ISpace sheet)
    {
      var section = VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines: v.Next(Table<KLine>().Named("lines"))));

      var sized = section.On(RowContaining("K-1 Lines 1-21")).Sized(Extent(3, 4)).Until(RowContaining("Portfolio Income"));
      var framed = section.On(RowContaining("K-1 Lines 1-21")).Until(RowContaining("Portfolio Income")).Sized(Extent(3, 4));

      var piped = On(RowContaining("K-1 Lines 1-21")).Sized(Extent(3, 4)).Until(RowContaining("Portfolio Income"))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("K-1 Lines 1-21")),
          Lines: v.Next(Table<KLine>().Named("lines"))));

      Judge.Same("extent inside the bound: the pipeline's only order agrees with today's good order",
        Safely(() => sized.Map(sheet)), Safely(() => piped.Map(sheet)));

      Console.WriteLine($"  - the other order today: {Safely(() => framed.Map(sheet))}");
      Judge.Note("That order is unspellable in the pipeline: a BoundStage has no Sized, so the extent cannot"
        + " become the frame a landmark is sought in. Hazard 4 dissolves into a grammar rule.");
    }

    private static string Safely(Func<object> read)
    {
      try
      {
        return Judge.Render(read());
      }
      catch (ProjectionException failure)
      {
        return $"FAILS: {failure.Message}";
      }
    }
  }
}
