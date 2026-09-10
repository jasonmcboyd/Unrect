using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The placement pipeline's laws: <c>Below(mark).Of(section)</c> and <c>section.Below(mark)</c> are
  /// one declaration, said two ways.
  /// <para>
  /// <strong>Why this is a law suite and not a feature suite.</strong> The pipeline ships no
  /// semantics of its own — a stage records the modifier the author wrote and replays it once, in
  /// declaration order, onto whatever the terminal builds. That is a claim about EVERY declaration,
  /// not about the ones anyone thought to write down, and it is the claim that would break silently
  /// if a stage ever started composing strategies itself instead of replaying: a movement
  /// <em>composes</em> onto a shape's own default offset where <c>OffsetBy</c> <em>replaces</em> it,
  /// so a pipeline that flattened both into one strategy would quietly change every declaration that
  /// relies on the difference and none of the ones a smoke test looks at.
  /// </para>
  /// <para>
  /// <strong>Sections.</strong> (1) The denotation sweep — the twelve comparisons the phase-2 smoke
  /// ran against <c>examples/investor-irr.xlsx</c>, restated over an in-memory grid and promoted to
  /// committed L3 pins. (2) Name capture through a terminal, per site and per hierarchy: the four
  /// factories that forward a <c>CallerArgumentExpression</c> are terminals here too, and a terminal
  /// that dropped the argument would label every occurrence with its own parameter name. (3)
  /// <c>Heading</c>'s L3-by-construction property — it contributes no node, so it can only ever fail
  /// in the caption's words. (4) The construction guards, including the one that matters most: the
  /// declared-over-declared refusal is UNREACHABLE from inside a pipeline. (5) The teaching stubs,
  /// present and speaking the library's words.
  /// </para>
  /// <para>
  /// The compiler diagnostics themselves — what a refused transition actually says at the call site
  /// — are not this suite's to keep and live as re-runnable code in
  /// <c>spike/PlacementGauntlet/MustNotCompilePipeline.cs</c>. What IS assertable about a refusal is
  /// section 5: that the stub is there, that it is an error rather than a warning, and that its
  /// sentence is the library's.
  /// </para>
  /// </summary>
  public class PlacementPipelineLawTests
  {
    private const string Transfer = "Cash Flows Using Transfer Date";

    private const string Inception = "Cash Flows using inception date";

    /// <summary>
    /// An IRR report in miniature — the shape the smoke read off the real workbook, small enough
    /// that every extent in the assertions below can be counted by eye:
    /// <code>
    /// r0   Investor IRR
    /// r1   IRR Details
    /// r2   Cash Flows Using Transfer Date
    /// r3   Alpha    100   Contribution
    /// r4   Alpha    200   Distribution
    /// r5
    /// r6   Beacon   300   Contribution
    /// r7   Cash Flows using inception date
    /// r8   Alpha    110   Contribution
    /// r9
    /// r10  Beacon   310   Contribution
    /// </code>
    /// Two caption-separated series of the same blank-separated blocks, which is what makes one
    /// hoisted <c>series</c> placed twice the natural declaration — and therefore what makes the two
    /// spellings of the placement worth comparing.
    /// </summary>
    private static ISpace Report() => Mixed(new object?[,]
    {
      { "Investor IRR", null, null },
      { "IRR Details", null, null },
      { Transfer, null, null },
      { "Alpha", 100m, "Contribution" },
      { "Alpha", 200m, "Distribution" },
      { null, null, null },
      { "Beacon", 300m, "Contribution" },
      { Inception, null, null },
      { "Alpha", 110m, "Contribution" },
      { null, null, null },
      { "Beacon", 310m, "Contribution" },
    });

    /// <summary>One investor's run of rows, read as its name and its height.</summary>
    private static IProjection<string> InvestorBlock()
      => Range(RowsWhileAnyValue(), block => $"{block[0, 0].GetString()}x{block.Height}");

    /// <summary>The repeated series both headings announce — hoisted, because it is declared once.</summary>
    private static IProjection<IReadOnlyList<string>> Series()
      => VerticalRepeat(InvestorBlock(), separatedBy: BlankRows());

    // --- 1. The denotation sweep: the pipeline against the modifiers it replays ----------------------
    //
    // Twelve pins, one per comparison the phase-2 smoke made. Each writes the same declaration twice
    // — postfix and pipeline — and compares at L3, which is value, offset, consumed extent, advance,
    // diagnostics in order, and the failure's sentence with its path and its subject.

    [Fact]
    public void TheBoundedSeriesReadsTheSameThroughEitherSpelling()
    {
      var space = Report();
      var series = Series();

      var postfix = series.Under(Caption("IRR Details"), Caption(Transfer)).Until(RowContaining(Inception));

      var pipeline = Heading("IRR Details").Heading(Transfer).Of(series).Until(RowContaining(Inception));

      AssertL3(Observe(postfix, space), Observe(pipeline, space));

      // Non-vacuity: the declaration really does read the first series and stop before the second.
      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, postfix.Map(space));
    }

    [Fact]
    public void TheUnboundedSeriesReadsTheSameThroughEitherSpelling()
    {
      var space = Report();
      var series = Series();

      var postfix = series.Under(Caption(Inception));
      var pipeline = Heading(Inception).Of(series);

      AssertL3(Observe(postfix, space), Observe(pipeline, space));

      Assert.Equal(new[] { "Alphax1", "Beaconx1" }, pipeline.Map(space));
    }

    [Fact]
    public void ABoundWrittenLeadingAndABoundWrittenPostfixAreOneDeclaration()
    {
      // The geography law's one genuine ambiguity: the bound's landmark sits BELOW the section, so
      // postfix reads well — and the bound is geometry, so the canonical stage order puts it ahead of
      // the headings. Both are spellable, and this is the pin that says they are the same words.
      var space = Report();
      var series = Series();

      var leading = Until(RowContaining(Inception)).Heading("IRR Details").Heading(Transfer).Of(series);
      var postfix = Heading("IRR Details").Heading(Transfer).Of(series).Until(RowContaining(Inception));

      AssertL3(Observe(leading, space), Observe(postfix, space));
    }

    [Fact]
    public void AndTheGeometryIsTheSameAndNotOnlyTheValue()
    {
      // AssertL3 already compares the three, so this pin exists to write the numbers down: a
      // comparison of two spellings that both drifted the same way would still pass, and a reader
      // has no way to tell from the sweep above what the placement actually resolved to.
      var space = Report();

      var pipeline = Heading("IRR Details").Heading(Transfer).Of(Series()).Until(RowContaining(Inception));
      var applied = pipeline.Apply(space);

      Assert.Equal("0x0", $"{applied.Offset.Size.Width}x{applied.Offset.Size.Height}");
      Assert.Equal("3x7", $"{applied.Consumed.Width}x{applied.Consumed.Height}");
      Assert.Equal("3x7", $"{applied.Advance.Width}x{applied.Advance.Height}");
    }

    [Fact]
    public void AWrongSecondHeadingFailsWithTheSamePathAndSentence()
    {
      var space = Report();
      var series = Series();

      AssertL3(
        Observe(series.Under(Caption("IRR Details"), Caption("Nope")), space),
        Observe(Heading("IRR Details").Heading("Nope").Of(series), space));

      var failure = Assert.Throws<ProjectionException>(() => Heading("IRR Details").Heading("Nope").Of(series).Map(space));

      Assert.Equal("Under -> Caption(\"Nope\")#2", failure.Path);
    }

    [Fact]
    public void AWrongFirstHeadingFailsWithTheSamePathAndSentence()
    {
      var space = Report();
      var series = Series();

      AssertL3(
        Observe(series.Under(Caption("Nope"), Caption(Transfer)), space),
        Observe(Heading("Nope").Heading(Transfer).Of(series), space));

      var failure = Assert.Throws<ProjectionException>(() => Heading("Nope").Heading(Transfer).Of(series).Map(space));

      Assert.Equal("Under -> Caption(\"Nope\")#1", failure.Path);
    }

    [Fact]
    public void TheWholeReportReadsIdenticallyThroughEitherSpelling()
    {
      var space = Report();
      var series = Series();

      var postfixReport = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        ByTransferDate = v.Next(series.Under(Caption("IRR Details"), Caption(Transfer)).Until(RowContaining(Inception))),
        ByInception = v.Next(series.Under(Caption(Inception))),
      });

      var pipelineReport = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        ByTransferDate = v.Next(Heading("IRR Details").Heading(Transfer).Of(series).Until(RowContaining(Inception))),
        ByInception = v.Next(Heading(Inception).Of(series)),
      });

      AssertL3(Observe(postfixReport, space), Observe(pipelineReport, space));

      var read = pipelineReport.Map(space);

      Assert.Equal("Investor IRR", read.Title);
      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, read.ByTransferDate);
      Assert.Equal(new[] { "Alphax1", "Beaconx1" }, read.ByInception);
    }

    [Fact]
    public void AndItsDiagnosticsAreTheSameOnesInTheSameOrder()
    {
      // A reading that leaves something over, so the comparison has diagnostics to compare: the
      // bounded series read at the ROOT accounts for rows 1-7 and says so about the rest. Verbatim,
      // because a diagnostic is a sentence a user reads and the pipeline must not reword it.
      var space = Report();
      var series = Series();

      var postfix = series.Under(Caption("IRR Details"), Caption(Transfer)).Until(RowContaining(Inception));
      var pipeline = Heading("IRR Details").Heading(Transfer).Of(series).Until(RowContaining(Inception));

      var left = postfix.MapWithDiagnostics(space);
      var right = pipeline.MapWithDiagnostics(space);

      Assert.NotEmpty(left.Diagnostics);
      Assert.Equal(
        left.Diagnostics.Select(diagnostic => diagnostic.ToString()),
        right.Diagnostics.Select(diagnostic => diagnostic.ToString()));
    }

    [Fact]
    public void TheRepeatStopRecipeIsTheSameDeclarationThroughThePipeline()
    {
      // The documented recipe: the anchor goes on the heading-and-content FLOW, so that running out
      // of headings ends the repetition. In the pipeline the anchor is the entry, which reads first
      // because it is furthest up the sheet — and the replay has to put it OUTSIDE the Under it
      // reads before, which is what Steps.Before exists for.
      var space = Report();
      var block = InvestorBlock();
      var mark = RowContaining("IRR Details");

      var postfixSection = block.Under(Caption("IRR Details")).On(mark);
      var pipelineSection = On(mark).Heading("IRR Details").Of(block);

      AssertL3(
        Observe(VerticalRepeat(postfixSection, separatedBy: BlankRows()), space),
        Observe(VerticalRepeat(pipelineSection, separatedBy: BlankRows()), space));
    }

    [Fact]
    public void AndAnchoringTheContentInsteadOfTheFlowIsADifferentDeclarationThePipelineCannotSpell()
    {
      // The negative half, stated as the specific difference: the wrong order anchors the CONTENT,
      // the flow's own placement always fits, and the repeat therefore fails loudly where the recipe
      // stops quietly. The pipeline cannot spell it — an anchor after a heading is refused — which is
      // the whole reason the prepend is load-bearing rather than a detail of the replay.
      var space = Report();
      var block = InvestorBlock();
      var mark = RowContaining("IRR Details");

      var recipe = VerticalRepeat(block.Under(Caption("IRR Details")).On(mark), separatedBy: BlankRows());
      var inside = VerticalRepeat(block.On(mark).Under(Caption("IRR Details")), separatedBy: BlankRows());

      // One occurrence: the caption owns r1, the block runs to the first blank row, and the next
      // iteration finds no second "IRR Details" to anchor on and stops — quietly, which is the recipe.
      Assert.Equal(new[] { "Cash Flows Using Transfer Datex3" }, recipe.Map(space));

      var failure = Assert.Throws<ProjectionException>(() => inside.Map(space));

      // Loudly, and inside the FIRST occurrence: the caption has already consumed the anchor row, so
      // the content's own anchor finds nothing below it and the flow's placement — which always fits
      // — never gets the chance to end the repetition.
      Assert.Equal("VerticalRepeat[0] -> Under -> Range#2", failure.Path);
      Assert.Contains("no row containing 'IRR Details' exists", failure.Message);
    }

    [Fact]
    public void SkipEmptyRowsAndColumnsIsExactlyItsTwoWords()
    {
      // The one-word entry for the instinct the default refuses. It is sugar, and this is the pin
      // that keeps it sugar: a second implementation of "step over the emptiness" would be a second
      // answer to a question the library answers once.
      var space = Report();
      var series = Series();

      AssertL3(
        Observe(series.AfterBlankRows().AfterBlankColumns(), space),
        Observe(SkipEmptyRowsAndColumns().Of(series), space));
    }

    [Fact]
    public void AHoistedProjectionPlacedByOfIsThePostfixModifier()
    {
      var space = Report();
      var series = Series();
      var mark = RowContaining(Transfer);

      AssertL3(Observe(series.On(mark), space), Observe(On(mark).Of(series), space));
    }

    [Fact]
    public void AndSoIsTheSameDeclarationSpelledOutAtTheTerminal()
    {
      // .Of places something already declared; a terminal declares it in place. Same words, same
      // reading — which is what lets a declaration move between the two spellings freely.
      var space = Report();
      var block = InvestorBlock();
      var mark = RowContaining(Transfer);

      AssertL3(
        Observe(VerticalRepeat(block, separatedBy: BlankRows()).On(mark), space),
        Observe(On(mark).VerticalRepeat(block, separatedBy: BlankRows()), space));
    }

    // --- 2. Name capture through a terminal, per site and per hierarchy -----------------------------
    //
    // Four factories forward a [CallerArgumentExpression], and each is a terminal on both stage
    // hierarchies — eight forwarding sites, eight pins. None of the identifiers below is called
    // `item` or `eachRow`: a test written with those would pass straight through the defect it
    // exists to catch, because a terminal that dropped the argument captures its OWN parameter name
    // and is indistinguishable wherever the caller's identifier happens to match.

    /// <summary>The ledger the capture pins fail over — a text column where a number is asked for.</summary>
    private static ISpace Ledger() => Mixed(new object?[,]
    {
      { "Fund", "Amount" },
      { "Alpha", 100m },
      { "Beta", 250m },
    });

    private static IRowLandmark Header() => RowContaining("Fund");

    /// <summary>A bind pointed at the column of fund names, so every record fails.</summary>
    private static IProjection<decimal> FundColumnAsANumber(CaptionMap captions) => Decimal().Right(captions["Fund"]);

    [Fact]
    public void AVerticalRepeatTerminalKeepsTheIdentifierItsItemWasWrittenAs()
    {
      var investorDetail = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).VerticalRepeat(investorDetail).Map(Ledger()));

      var postfix = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(investorDetail).On(Header()).Map(Ledger()));

      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", throughPipeline.Path);
      Assert.Equal(postfix.Path, throughPipeline.Path);
      Assert.Equal(postfix.Message, throughPipeline.Message);
    }

    [Fact]
    public void AndSoDoesAHorizontalRepeatTerminal()
    {
      var quarterlyColumn = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).HorizontalRepeat(quarterlyColumn).Map(Ledger()));

      var postfix = Assert.Throws<ProjectionException>(
        () => HorizontalRepeat(quarterlyColumn).On(Header()).Map(Ledger()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", throughPipeline.Path);
      Assert.Equal(postfix.Path, throughPipeline.Path);
      Assert.Equal(postfix.Message, throughPipeline.Message);
    }

    [Fact]
    public void AndATableRowSlotTerminal()
    {
      var allocationRow = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      var postfix = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: allocationRow).On(Header()).Map(Ledger()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", throughPipeline.Path);
      Assert.Equal(postfix.Path, throughPipeline.Path);
      Assert.Equal(postfix.Message, throughPipeline.Message);
    }

    [Fact]
    public void AndATableBindTerminal_LabelledByTheMethodGroupItWasPassedAs()
    {
      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Ledger()));

      var postfix = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: FundColumnAsANumber).On(Header()).Map(Ledger()));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", throughPipeline.Path);
      Assert.Equal(postfix.Path, throughPipeline.Path);
      Assert.Equal(postfix.Message, throughPipeline.Message);
    }

    [Fact]
    public void AScopedVerticalRepeatTerminalCapturesTheSameWay()
    {
      // The scoped hierarchy repeats every terminal rather than inheriting them, because each has to
      // hand the demand out in its result type — so each is a SECOND forwarding site, and the
      // capture has to be forwarded twice over.
      var investorDetail = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => Over<ISpace>().On(Header()).VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", throughPipeline.Path);
    }

    [Fact]
    public void AndAScopedHorizontalRepeatTerminal()
    {
      var quarterlyColumn = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => Over<ISpace>().On(Header()).HorizontalRepeat(quarterlyColumn).Map(Ledger()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", throughPipeline.Path);
    }

    [Fact]
    public void AndAScopedTableRowSlotTerminal()
    {
      var allocationRow = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => Over<ISpace>().On(Header()).Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", throughPipeline.Path);
    }

    [Fact]
    public void AndAScopedTableBindTerminal()
    {
      var throughPipeline = Assert.Throws<ProjectionException>(
        () => Over<ISpace>().On(Header()).Table(headerRows: 1, eachRow: ScopedFundColumnAsANumber).Map(Ledger()));

      Assert.Equal("Table[0] -> 'ScopedFundColumnAsANumber' (Decimal)", throughPipeline.Path);
    }

    /// <summary>The scoped bind's method group — a bind returning a demanding projection.</summary>
    private static IProjection<ISpace, decimal> ScopedFundColumnAsANumber(CaptionMap captions)
      => Decimal().Right(captions["Fund"]);

    // --- 3. Heading is L3-by-construction ------------------------------------------------------------

    [Fact]
    public void AHeadingContributesNoNodeSoItFailsInTheCaptionsOwnWords()
    {
      // The word Heading appears nowhere a user can see it: the terminal mints the Caption leaves the
      // replay hands to Under, so the path, the subject and the sentence are the hand-written
      // declaration's exactly. This is what "structure, not a value" costs and what it buys.
      var space = Report();

      var failure = Assert.Throws<ProjectionException>(() => Heading("Nope").Of(Series()).Map(space));

      Assert.Equal("Under -> Caption(\"Nope\")#1", failure.Path);
      Assert.Equal("Caption(\"Nope\")#1", failure.Subject);
      Assert.DoesNotContain("Heading", failure.Message, StringComparison.Ordinal);
      Assert.False(failure.IsFault);
    }

    [Fact]
    public void ChainedHeadingsAccumulateIntoOneFlowRatherThanNestedOnes()
    {
      // Two headings above one section are one statement. The path is where that is observable: one
      // Under segment, two captions numbered within it.
      var space = Report();

      var failure = Assert.Throws<ProjectionException>(
        () => Heading("IRR Details").Heading("Nope").Of(Series()).Map(space));

      Assert.Equal(1, Occurrences(failure.Path, "Under"));
      Assert.Equal("Under -> Caption(\"Nope\")#2", failure.Path);

      // ...and it is the same declaration as the one Under with both captions in it.
      AssertL3(
        Observe(Series().Under(Caption("IRR Details"), Caption(Transfer)), space),
        Observe(Heading("IRR Details").Heading(Transfer).Of(Series()), space));
    }

    [Fact]
    public void WhereasLayeringIsNestingAndIsSpelledAsNesting()
    {
      // The negative pin, as the SPECIFIC difference: a heading over a heading-and-section is two
      // flows, and the second Under in the path is where the reader sees it.
      var space = Report();

      var nested = Assert.Throws<ProjectionException>(
        () => Heading("IRR Details").Of(Heading("Nope").Of(Series())).Map(space));

      // Two Under segments, and the inner one carries an ordinal because it is a CHILD of the outer
      // flow — which is precisely the difference: a chained heading is a sibling caption, a layered
      // one is a section inside a section.
      Assert.Equal(2, Occurrences(nested.Path, "Under"));
      Assert.Equal("Under -> Under#2 -> Caption(\"Nope\")#1", nested.Path);
    }

    // --- 4. The construction guards ------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AHeadingCannotBeBlankOnAnyOfTheThreeSurfaces(string? text)
    {
      // Null lands here rather than on ArgumentNullException deliberately: the question a heading
      // answers is "what does this section say", and no text is no text however it was spelled.
      foreach (var refusal in new[]
      {
        Assert.Throws<ArgumentException>(() => Heading(text!)),
        Assert.Throws<ArgumentException>(() => Over<ISpace>().Heading(text!)),
        Assert.Throws<ArgumentException>(() => ProjectionBuilders<ISpace>.Heading(text!)),
        Assert.Throws<ArgumentException>(() => Heading("IRR Details").Heading(text!)),
        Assert.Throws<ArgumentException>(() => Until(RowContaining(Inception)).Heading(text!)),
      })
      {
        Assert.Equal("text", refusal.ParamName);
        Assert.Contains("cannot be blank", refusal.Message);
      }
    }

    [Fact]
    public void TheRichestPipelineWritesEachSlotOnceSoTheDeclaredOverDeclaredRefusalCannotFire()
    {
      // Every slot the pipeline has, in one declaration: the anchor (its entry), a movement composing
      // onto it, the extent, the bound, the headings, and a terminal. The stage types make a second
      // write unspellable, so the replay applies each slot exactly once — which is the whole reason
      // the shipped declared-over-declared refusal can never fire from inside a pipeline. If the
      // replay ever wrote a slot twice, THIS is where it would throw ArgumentException instead of
      // reading.
      var space = Report();
      var series = Series();
      var mark = RowContaining("IRR Details");

      var pipeline = On(mark)
        .Down(1)
        .Sized(Extent(3, 5))
        .Until(RowContaining("Nowhere"), orEnd: true)
        .Heading(Transfer)
        .Of(series);

      var postfix = series
        .Under(Caption(Transfer))
        .On(mark)
        .Down(1)
        .Sized(Extent(3, 5))
        .Until(RowContaining("Nowhere"), orEnd: true);

      AssertL3(Observe(postfix, space), Observe(pipeline, space));

      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, pipeline.Map(space));
    }

    [Fact]
    public void AndTheRefusalItCannotReachIsStillReal()
    {
      // The other half, so the pin above is not merely asserting that nothing happens: written
      // postfix, a second position on one projection is refused at construction, in the words the
      // pipeline's SecondAnchor stub echoes.
      var refusal = Assert.Throws<ArgumentException>(
        () => Series().On(RowContaining("IRR Details")).On(RowContaining(Inception)));

      Assert.Equal("projection", refusal.ParamName);
      Assert.Contains("already declares where it starts", refusal.Message);
    }

    // --- 5. The teaching stubs -----------------------------------------------------------------------

    /// <summary>Every stage type, plain and scoped, so a refusal cannot go missing from one half.</summary>
    public static TheoryData<string> TheStageTypes
    {
      get
      {
        var data = new TheoryData<string>();

        foreach (var stage in Stages.Keys.OrderBy(name => name, StringComparer.Ordinal))
          data.Add(stage);

        return data;
      }
    }

    /// <summary>
    /// One instance of each stage, reached the way a declaration reaches it. The two abstract stages
    /// are read through a concrete one, since a member declared on <c>UnboundedStage</c> is refused
    /// wherever it is inherited.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (Type Type, object Instance)> Stages =
      new Dictionary<string, (Type, object)>(StringComparer.Ordinal)
      {
        ["PlacementStage"] = (typeof(PlacementStage), Down(1)),
        ["UnboundedStage"] = (typeof(UnboundedStage), Down(1)),
        ["OffsetStage"] = (typeof(OffsetStage), Down(1)),
        ["OffsetAndSizeStage"] = (typeof(OffsetAndSizeStage), Down(1).Sized(WholeExtent())),
        ["BoundStage"] = (typeof(BoundStage), Until(RowContaining("IRR Details"))),
        ["HeadingStage"] = (typeof(HeadingStage), Heading("IRR Details")),
        ["PlacementStage<TSpace>"] = (typeof(PlacementStage<ISpace>), Over<ISpace>().Down(1)),
        ["UnboundedStage<TSpace>"] = (typeof(UnboundedStage<ISpace>), Over<ISpace>().Down(1)),
        ["OffsetStage<TSpace>"] = (typeof(OffsetStage<ISpace>), Over<ISpace>().Down(1)),
        ["OffsetAndSizeStage<TSpace>"] = (typeof(OffsetAndSizeStage<ISpace>), Over<ISpace>().Down(1).Sized(WholeExtent())),
        ["BoundStage<TSpace>"] = (typeof(BoundStage<ISpace>), Over<ISpace>().Until(RowContaining("IRR Details"))),
        ["HeadingStage<TSpace>"] = (typeof(HeadingStage<ISpace>), Over<ISpace>().Heading("IRR Details")),
      };

    /// <summary>The seven sentences a stage is allowed to refuse with, written once in the library.</summary>
    private static readonly IReadOnlyList<string> TheRefusals = new[]
    {
      PipelineRefusals.SecondAnchor,
      PipelineRefusals.HeadingIsTheAnchor,
      PipelineRefusals.OffsetComesFirst,
      PipelineRefusals.ExtentsDoNotStack,
      PipelineRefusals.ProjectionHasOneEnd,
      PipelineRefusals.BoundFramesTheExtent,
      PipelineRefusals.GeometryComesBeforeTheHeadings,
    };

    [Theory]
    [MemberData(nameof(TheStageTypes))]
    public void EveryRefusedStageMemberIsAnErrorAndSaysTheLibrarysWordsTwice(string stage)
    {
      // The ship requirement, made a pin: a refused transition must say the library's sentence rather
      // than the compiler's babble about type parameters and implicit reference conversions — which
      // is what OMITTING the member would produce, since the placement modifiers are self-typed
      // extension candidates on every stage. So each refusal is checked three ways: it is an ERROR
      // (a warning would let the contradiction ship), its message is one the library wrote, and the
      // body says the same thing to anyone who reaches it by reflection.
      var (type, instance) = Stages[stage];

      foreach (var member in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
      {
        var refusal = member.GetCustomAttribute<ObsoleteAttribute>();

        if (refusal is null)
          continue;

        Assert.True(refusal.IsError, $"{stage}.{member.Name} refuses as a warning rather than an error");
        Assert.Contains(refusal.Message, TheRefusals);

        var thrown = Assert.Throws<NotSupportedException>(() => Invoke(member, instance));

        Assert.Equal(refusal.Message, thrown.Message);
      }
    }

    [Fact]
    public void AndTheStubsAreWhereTheTaxonomySaysTheyAre()
    {
      // The census, so a stub that quietly disappeared — taking its sentence with it and leaving the
      // compiler to explain the refusal in its own words — is noticed. Counted per stage rather than
      // in total, because that is where a reader can check the claim: an unbounded pipeline refuses
      // only a second anchor (5); a sized one refuses the movements as well (5, all of them); a
      // bounded one refuses a second end, an extent and the anchors (10); a headed one refuses
      // everything but another heading (12).
      var counted = Stages
        .Where(stage => stage.Key.Contains("<") == false)
        .ToDictionary(stage => stage.Key, stage => Refusals(stage.Value.Type), StringComparer.Ordinal);

      Assert.Equal(
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
          ["PlacementStage"] = 0,
          ["UnboundedStage"] = 5,
          ["OffsetStage"] = 0,
          ["OffsetAndSizeStage"] = 5,
          ["BoundStage"] = 10,
          ["HeadingStage"] = 12,
        },
        counted);

      // The scoped half repeats every one of them, which is the price of a stage that carries a
      // demand: nothing is inherited, so nothing can be forgotten in only one hierarchy.
      foreach (var stage in new[] { "UnboundedStage", "OffsetAndSizeStage", "BoundStage", "HeadingStage" })
        Assert.Equal(Refusals(Stages[stage].Type), Refusals(Stages[stage + "<TSpace>"].Type));
    }

    private static int Refusals(Type stage)
      => stage
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Count(member => member.GetCustomAttribute<ObsoleteAttribute>() is not null);

    /// <summary>
    /// Calls a refused member with whatever its parameters need. Every stub throws before looking at
    /// an argument, so nulls and defaults are enough — and using them is the point: a stub that
    /// validated first would be doing work a refusal has no business doing.
    /// </summary>
    private static void Invoke(MethodInfo member, object instance)
    {
      var arguments = member.GetParameters()
        .Select(parameter => parameter.ParameterType.IsValueType
          ? Activator.CreateInstance(parameter.ParameterType)
          : null)
        .ToArray();

      try
      {
        member.Invoke(instance, arguments);
      }
      catch (TargetInvocationException invocation) when (invocation.InnerException is not null)
      {
        throw invocation.InnerException;
      }
    }
  }
}
