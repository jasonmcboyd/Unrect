using System;
using System.Linq;

using PlacementGauntlet.Staged;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario H — <b>Heading: the fossil pair dissolved.</b> One stage word where
  /// <c>Under</c> + <c>Caption</c> stood.
  /// <para>
  /// The trial's claim is that <c>Heading</c> is a SPELLING of what the two fossils did together:
  /// the terminal replays <c>projection.Under(Caption(text), …)</c> innermost, so every read below
  /// should agree at L3 by construction. Four reads test that and one thing more — where the
  /// dissolution FIGHTS BACK, which is the finding this scenario exists to surface.
  /// </para>
  /// </summary>
  public static class ScenarioH
  {
    private const string Inception = "Cash Flows using inception date";

    public static void Run()
    {
      Judge.Section("Scenario H — Heading, the fossil pair dissolved");

      TheIrrSeries();
      TheRepeatStopRecipe();
      TheSurvivingCaptionCase();
      LayeredComposition();
      InAZeroPrefixFile();
      WhereItFightsBack();
    }

    // --- Read 5: Entry C integration ------------------------------------------------------------------

    private static void InAZeroPrefixFile()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investor-irr.xlsx"), "IRR");

      Judge.SameL2("read 5 — the same script through ProjectionBuilders<ISpace>, no prefixes and no Caption",
        ScenarioCPlain.Report.Apply(sheet), ScenarioHEntryC.Report.Apply(sheet));

      Judge.Same("read 5 at L3 — diagnostics, verbatim",
        ScenarioCPlain.Report.MapWithDiagnostics(sheet).Diagnostics.Select(d => d.ToString()).ToList(),
        ScenarioHEntryC.Report.MapWithDiagnostics(sheet).Diagnostics.Select(d => d.ToString()).ToList());

      // The bound leading (ruling 1's canonical order) against the bound postfix (the geography law).
      // Both replay the same two calls in the same order, so they are one declaration spelled twice.
      var postfix = Place.Heading("IRR Details")
        .Heading("Cash Flows Using Transfer Date")
        .Of(VerticalRepeat(Table<CashFlow>(), separatedBy: BlankRows()))
        .Until(RowContaining(Inception));

      Judge.SameL2("read 5 — bound LEADING and bound POSTFIX are the same declaration",
        postfix.Apply(sheet), ScenarioHEntryC.BoundLeading.Apply(sheet));

      Judge.Note("So Heading-as-entry composes with Until either way round, and ruling 1's canonical order is a"
        + " READING preference rather than a semantic one: Until(l).Heading(a).Heading(b).Of(x) and"
        + " Heading(a).Heading(b).Of(x).Until(l) replay Under-then-Until identically. Evidence for the open"
        + " owner call on where a bound sits, not a position taken.");
      Judge.Note("The Entry C file imports no Caption and writes none — the discarded leaf is gone from the"
        + " declaration surface entirely, which is what the dissolution was for.");
    }

    // --- Read 1: the investor-irr series, both halves -----------------------------------------------

    private static void TheIrrSeries()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investor-irr.xlsx"), "IRR");

      var investorBlock = Table<CashFlow>();
      var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      // TODAY — the fossil pair: a postfix modifier taking leaves built to be discarded.
      var todayBounded = irrDetails
        .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
        .Until(RowContaining(Inception));

      var todayUnbounded = irrDetails.Under(Caption(Inception));

      // HEADING — ruling 1's canonical order: bound, then headings in document order, then subject.
      var headingBounded = Place.Until(RowContaining(Inception))
        .Heading("IRR Details")
        .Heading("Cash Flows Using Transfer Date")
        .Of(irrDetails);

      var headingUnbounded = Place.Heading(Inception).Of(irrDetails);

      Judge.SameL2("read 1 — the bounded series: value and geometry",
        todayBounded.Apply(sheet), headingBounded.Apply(sheet));

      Judge.SameL2("read 1 — the unbounded series: value and geometry",
        todayUnbounded.Apply(sheet), headingUnbounded.Apply(sheet));

      // L3, the account-of-itself half. Two headings chained must accumulate into ONE replayed
      // Under call: nesting them would build two flows and say so in the path, which is the
      // difference this pin exists to catch.
      var todayBroken = irrDetails.Under(Caption("IRR Details"), Caption("Nope")).Until(RowContaining(Inception));
      var headingBroken = Place.Until(RowContaining(Inception)).Heading("IRR Details").Heading("Nope").Of(irrDetails);

      Judge.SameFailure("read 1 at L3 — a wrong second heading fails with the same path and sentence",
        () => todayBroken.Map(sheet), () => headingBroken.Map(sheet));

      Judge.SameFailure("read 1 at L3 — a wrong FIRST heading fails with the same path and sentence",
        () => irrDetails.Under(Caption("Nope"), Caption("Cash Flows Using Transfer Date")).Map(sheet),
        () => Place.Heading("Nope").Heading("Cash Flows Using Transfer Date").Of(irrDetails).Map(sheet));

      // And the whole script, diagnostics included.
      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title: v.Next(Text()),
        Fund: v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId: v.Next(Text())));

      var summary = Table<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

      var todayReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(reportHeader),
        Summary = v.Next(summary),
        ByTransferDate = v.Next(todayBounded),
        ByInception = v.Next(todayUnbounded),
      });

      var headingReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(reportHeader),
        Summary = v.Next(summary),
        ByTransferDate = v.Next(headingBounded),
        ByInception = v.Next(headingUnbounded),
      });

      var todayMapped = todayReport.MapWithDiagnostics(sheet);
      var headingMapped = headingReport.MapWithDiagnostics(sheet);

      Judge.Same("read 1 at L3 — the whole investor-irr script: value", todayMapped.Value, headingMapped.Value);
      Judge.Same("read 1 at L3 — the whole investor-irr script: diagnostics, verbatim",
        todayMapped.Diagnostics.Select(d => d.ToString()).ToList(),
        headingMapped.Diagnostics.Select(d => d.ToString()).ToList());

      Judge.Note("L3 by construction, and the pins confirm it: the stage holds TEXT and mints the Caption leaves at"
        + " replay time, so the description a failure prints is the leaf's own — identical to the one a hand-written"
        + " Under prints. The heading's description naming question never arises, because the heading has no node.");
    }

    // --- Read 2: the repeat-stop recipe -------------------------------------------------------------
    //
    // The documented recipe puts the anchor on the heading-and-content FLOW so that running out of
    // anchors ends the repetition. Under the geography law it reads left to right; under Heading it
    // reads the same way with one word less.

    private static void TheRepeatStopRecipe()
    {
      var sheet = Sheets.Regions();

      var regionMark = RowContaining("Region A");
      var lines = Table<Line>();

      var today = lines.Under(Caption("Region A")).On(regionMark);
      var heading = Place.On(regionMark).Heading("Region A").Of(lines);

      Judge.SameL2("read 2 — the recipe: anchor on the flow, either spelling", today.Apply(sheet), heading.Apply(sheet));

      // The negative: anchoring the CONTENT instead of the flow is a different declaration, and the
      // pipeline still cannot spell it — the prepend is as load-bearing for Heading as for Under.
      var anchoredInside = lines.On(regionMark).Under(Caption("Region A"));

      Judge.Different("read 2's negative — anchoring the content instead of the flow reads differently",
        Attempt(() => heading.Map(sheet)), Attempt(() => anchoredInside.Map(sheet)));

      Judge.Note("So Heading inherits the recipe intact: On(mark).Heading(\"…\").Of(lines) replays as"
        + " lines.Under(Caption(\"…\")).On(mark), never as lines.On(mark).Under(Caption(\"…\")).");

      // A heading is the text a section announces itself by, so the empty one is refused at
      // construction — the same class of guard Step.Under's empty-array check already is.
      Judge.Refused("a blank heading is refused when the declaration is built", () => Place.Heading("   "));
    }

    // --- Read 3: the surviving Caption case ---------------------------------------------------------
    //
    // Caption-the-leaf's one remaining home: a section that CAPTURES its heading's text as data. The
    // K-1 KSection does, and it is unchanged; its sibling discards the text and becomes a Heading.
    // The read must show the two coexisting coherently, because that is the shape of the retirement
    // being proposed — the discard case leaves, the capture case stays.

    private static void TheSurvivingCaptionCase()
    {
      var sheet = Sheets.K1();

      var kLines = Table(headerRows: 1, eachRow: captions => Overlay(o => new KLine(
        Code: o.Next(Text().Right(captions["Line"])),
        Amount: o.Next(Decimal().Right(captions["Amount"])))));

      // CAPTURES the text: Caption is a leaf here because the record wants what it read. Unchanged
      // by the dissolution — not grandfathered, but because this is what a value-yielding leaf IS.
      var capturing = VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("K-1 Lines 1-21")),
          Lines: v.Next(kLines)))
        .On(RowContaining("K-1 Lines 1-21"))
        .Until(RowContaining("Portfolio Income"));

      // DISCARDS the text: no record field for it, so the fossil is visible — a Caption built to be
      // thrown away. This is the half Heading replaces.
      var discardingToday = kLines
        .Under(Caption("Portfolio Income"))
        .On(RowContaining("Portfolio Income"))
        .Until(RowContaining("Totals"));

      var discardingHeading = Place.On(RowContaining("Portfolio Income"))
        .Until(RowContaining("Totals"))
        .Heading("Portfolio Income")
        .Of(kLines);

      Judge.SameL2("read 3 — the discard case: Heading reads what Under(Caption(…)) read",
        discardingToday.Apply(sheet), discardingHeading.Apply(sheet));

      // The two side by side in ONE document — the coexistence the retirement proposal turns on.
      var report = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        Lines = v.Next(capturing),
        Portfolio = v.Next(discardingHeading),
      });

      var mapped = report.MapWithDiagnostics(sheet);

      Judge.Same("read 3 — capture and discard in one document: the captured text is still there",
        "K-1 Lines 1-21", mapped.Value.Lines.Caption);
      Judge.Note($"And the discarding sibling reads {Judge.Render(mapped.Value.Portfolio)} with no caption anywhere in"
        + " its type — which is the point: the text was structure, and structure is not a value.");
      Judge.Note("The two read coherently side by side because they are the SAME machinery: Caption locates, asserts"
        + " and consumes in both, and the only difference is whether a cursor takes what it yielded.");
    }

    // --- Read 4: layered composition ----------------------------------------------------------------
    //
    // The double-Under question evaporates. Two headings at ONE layer chain (read 1); headings at
    // DIFFERENT layers nest through .Of(...), which is the grammar law — one dot-chain, one subject;
    // content enters through parentheses.

    private static void LayeredComposition()
    {
      var sheet = Sheets.K1();

      var kLines = Table(headerRows: 1, eachRow: captions => Overlay(o => new KLine(
        Code: o.Next(Text().Right(captions["Line"])),
        Amount: o.Next(Decimal().Right(captions["Amount"])))));

      var section = Place.Heading("Portfolio Income").Of(kLines);
      var document = Place.Heading("Partner K-1").Of(section);

      var today = kLines.Under(Caption("Portfolio Income")).Under(Caption("Partner K-1"));

      Judge.SameL2("read 4 — headings at two layers: value and geometry", today.Apply(sheet), document.Apply(sheet));

      Judge.Note("Ledger entry (n) recorded x.Under(a).Under(b) as 'unusual but not contradictory' and the pipeline"
        + " refused the stacked form. Under Heading the question dissolves rather than being answered: chaining is"
        + " ONE layer's headings in document order, nesting is TWO layers, and the two spellings cannot be confused"
        + " because one is dots and the other is parentheses.");
    }

    // --- Where the dissolution fights back -----------------------------------------------------------

    private static void WhereItFightsBack()
    {
      var sheet = Sheets.Regions();

      var lines = Table<Line>();

      // The repeat-stop recipe as the corpus actually writes it: the heading text VARIES per
      // occurrence, so it is read rather than asserted. Heading takes a string, so it cannot be
      // written here at all — and the Under entry is what remains.
      var regionMark = RowWithCell(cell => cell.Kind == CellKind.Text && cell.GetString().StartsWith("Region "));
      var regionName = Row(cells => cells[0].GetString());

      var discovered = Place.On(regionMark).Under(regionName).Of(lines);
      var regions = VerticalRepeat(discovered, separatedBy: BlankRows());

      var read = regions.Map(sheet);

      Judge.Note($"FINDING — Heading cannot spell a DISCOVERED heading. {Judge.Render(read)} is read by"
        + " On(regionMark).Under(regionName).Of(lines), where regionName is Row(cells => cells[0].GetString()):"
        + " the text varies per occurrence, so the row is consumed without being asserted.");
      Judge.Note("That is a THIRD use of Under, beside assert-and-discard (which Heading takes) and capture (which"
        + " Caption keeps): consume-without-asserting. Heading(string) refuses it by type — ledgered as (x) — so"
        + " retiring the Under ENTRY outright would delete a spelling the corpus uses today.");
      Judge.Note("Two exits exist and neither is free: a Heading overload taking a projection (which reinstates the"
        + " fossil argument the trial removed), or a separate word for the consume-without-asserting case. The trial"
        + " does not choose; it records that the choice exists.");

      // And the second place it fights: a heading whose text the record wants is NOT a heading at all
      // under this vocabulary — it is a Caption leaf inside the flow. Read 3 shows that coexisting;
      // what it costs is that one document concept has two spellings decided by the RESULT type.
      Judge.Note("Second friction: which word to write is decided by whether the RESULT wants the text, not by what"
        + " the document looks like. Two identical-looking heading rows spell differently — Heading(\"X\") or"
        + " v.Next(Caption(\"X\")) — and only the record type says which. That is coherent, and it is a thing to teach.");
    }

    private static string Attempt(Func<object?> read)
    {
      try
      {
        return "read " + Judge.Render(read());
      }
      catch (ProjectionException failure)
      {
        return $"FAILS: {failure.Message.Split('\n')[0]}";
      }
    }
  }
}
