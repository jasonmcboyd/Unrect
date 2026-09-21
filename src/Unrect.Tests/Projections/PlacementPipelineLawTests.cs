using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The placement pipeline's laws. Geometry is spelled only through the pipeline now — the postfix
  /// modifiers that once gave a second spelling are retired — so what once were pipeline-vs-postfix
  /// differentials are DIRECT pins of the pipeline's reading, and the arms that compared two pipeline
  /// spellings (the sugar against its expansion, the terminal against <c>.Of</c>) remain comparisons.
  /// <para>
  /// <strong>Why this is a law suite and not a feature suite.</strong> The pipeline ships no
  /// semantics of its own — a stage records the geometry the author wrote and applies it once, in
  /// declaration order, onto whatever the terminal builds. That is a claim about EVERY declaration,
  /// not about the ones anyone thought to write down, and it is the claim that would break silently
  /// if a stage ever started composing strategies itself instead of applying them plainly: a movement
  /// <em>composes</em> onto a shape's own default offset where <c>OffsetBy</c> <em>replaces</em> it,
  /// so a pipeline that flattened both into one strategy would quietly change every declaration that
  /// relies on the difference and none of the ones a smoke test looks at.
  /// </para>
  /// <para>
  /// <strong>Sections.</strong> (1) The denotation sweep — the comparisons the phase-2 smoke
  /// ran against <c>examples/investor-irr.xlsx</c>, restated over an in-memory grid and promoted to
  /// committed pins. (2) Name capture through a terminal, per site and per hierarchy: the four
  /// factories that forward a <c>CallerArgumentExpression</c> are terminals here too, and a terminal
  /// that dropped the argument would label every occurrence with its own parameter name. (3)
  /// <c>Heading</c>'s L3-by-construction property — it mints real caption leaves, so it can only ever
  /// fail in the caption's words. (4) The construction guards, including the one that matters most: the
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
    private static ICellSpace Report() => Mixed(new object?[,]
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
    private static IProjectionDefinition<ICellSpace, string> InvestorBlock()
      => Range(RowsWhileAnyValue(), block => $"{block[0, 0].Text()}x{block.Height}");

    /// <summary>The repeated series both headings announce — hoisted, because it is declared once.</summary>
    private static IProjectionDefinition<ICellSpace, IReadOnlyList<string>> Series()
      => VerticalRepeat(InvestorBlock(), separatedBy: BlankRows());

    // --- 1. The denotation sweep: the pipeline against the modifiers it replays ----------------------
    //
    // Twelve pins, one per comparison the phase-2 smoke made. Each writes the same declaration twice
    // — postfix and pipeline — and compares at L3, which is value, offset, consumed extent, advance,
    // diagnostics in order, and the failure's sentence with its path and its subject.

    [Fact]
    public void TheBoundedSeriesReadsThroughThePipeline()
    {
      // Geometry is spelled only through the pipeline now, so the bound leads and the headings follow:
      // the bound is geometry and the canonical stage order puts it ahead of the headings. (The retired
      // postfix twin this used to be compared against is gone; this is the direct pin of the reading.)
      var space = Report();
      var series = Series();

      var pipeline = Until(RowContaining(Inception)).Heading("IRR Details").Heading(Transfer).Of(series);

      // Non-vacuity: the declaration really does read the first series and stop before the second.
      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, pipeline.Map(space));
    }

    [Fact]
    public void TheUnboundedSeriesReadsThroughThePipeline()
    {
      var space = Report();
      var series = Series();

      var pipeline = Heading(Inception).Of(series);

      Assert.Equal(new[] { "Alphax1", "Beaconx1" }, pipeline.Map(space));
    }

    [Fact]
    public void TheBoundLeadsAndTheHeadingsFollowInTheCanonicalOrder()
    {
      // The geography law's one genuine ambiguity: the bound's landmark sits BELOW the section, and the
      // bound is geometry, so the canonical stage order puts it ahead of the headings —
      // Until(...).Heading(...).Heading(...).Of(series). This pins that spelling reads the bounded
      // series; the retired postfix-Until twin it used to be compared against is gone.
      var space = Report();
      var series = Series();

      var leading = Until(RowContaining(Inception)).Heading("IRR Details").Heading(Transfer).Of(series);

      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, leading.Map(space));
    }

    [Fact]
    public void AndTheGeometryIsTheSameAndNotOnlyTheValue()
    {
      // The numbers written down: a reading whose value looked right could still have resolved its
      // placement wrong, and the value pin above would not catch it.
      var space = Report();

      var pipeline = Until(RowContaining(Inception)).Heading("IRR Details").Heading(Transfer).Of(Series());
      var applied = pipeline.Apply(space);

      Assert.Equal("0x0", $"{applied.Offset.Size.Width}x{applied.Offset.Size.Height}");
      Assert.Equal("3x7", $"{applied.Consumed.Width}x{applied.Consumed.Height}");
      Assert.Equal("3x7", $"{applied.Advance.Width}x{applied.Advance.Height}");
    }

    [Fact]
    public void AWrongSecondHeadingFailsInTheCaptionsWords()
    {
      var space = Report();
      var series = Series();

      var failure = Assert.Throws<ProjectionException>(() => Heading("IRR Details").Heading("Nope").Of(series).Map(space));

      Assert.Equal("Heading -> Caption(\"Nope\")#2", failure.Path);
    }

    [Fact]
    public void AWrongFirstHeadingFailsInTheCaptionsWords()
    {
      var space = Report();
      var series = Series();

      var failure = Assert.Throws<ProjectionException>(() => Heading("Nope").Heading(Transfer).Of(series).Map(space));

      Assert.Equal("Heading -> Caption(\"Nope\")#1", failure.Path);
    }

    [Fact]
    public void TheWholeReportReadsThroughThePipeline()
    {
      var space = Report();
      var series = Series();

      var pipelineReport = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        ByTransferDate = v.Next(Until(RowContaining(Inception)).Heading("IRR Details").Heading(Transfer).Of(series)),
        ByInception = v.Next(Heading(Inception).Of(series)),
      });

      var read = pipelineReport.Map(space);

      Assert.Equal("Investor IRR", read.Title);
      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, read.ByTransferDate);
      Assert.Equal(new[] { "Alphax1", "Beaconx1" }, read.ByInception);
    }

    [Fact]
    public void AndItLeavesTheTrailingRowsOverAndSaysSo()
    {
      // A reading that leaves something over: the bounded series read at the ROOT accounts for rows 1-7
      // and says so about the rest. (The retired postfix twin this used to compare its diagnostics
      // against is gone; what survives is that the pipeline still raises the unconsumed-space
      // diagnostic rather than swallowing it.)
      var space = Report();
      var series = Series();

      var pipeline = Until(RowContaining(Inception)).Heading("IRR Details").Heading(Transfer).Of(series);

      var mapped = pipeline.MapWithDiagnostics(space);

      Assert.NotEmpty(mapped.Diagnostics);
    }

    [Fact]
    public void TheRepeatStopRecipeIsTheSameDeclarationThroughThePipeline()
    {
      // The documented recipe: the anchor goes on the heading-and-content FLOW, so that running out
      // of headings ends the repetition. In the pipeline the anchor is the entry, which reads first
      // because it is furthest up the sheet — and the pipeline has to put it OUTSIDE the Heading it
      // reads before, which is what Steps.Before exists for.
      var space = Report();
      var block = InvestorBlock();
      var mark = RowContaining("IRR Details");

      // The anchor leads, so it lands on the heading-and-content FLOW: On(mark).Heading(...).Of(block).
      var pipelineSection = On(mark).Heading("IRR Details").Of(block);

      Assert.Equal(
        new[] { "Cash Flows Using Transfer Datex3" },
        VerticalRepeat(pipelineSection, separatedBy: BlankRows()).Map(space));
    }

    [Fact]
    public void AndAnchoringTheContentInsteadOfTheFlowIsADifferentDeclaration()
    {
      // The negative half, stated as the specific difference: leading with the anchor lands it on the
      // flow (the recipe); NESTING it inside the heading anchors the CONTENT instead, the flow's own
      // placement always fits, and the repeat therefore fails loudly where the recipe stops quietly.
      // The flat pipeline cannot anchor the content — an anchor after a heading is refused — so the
      // content-anchored spelling is only reachable by nesting the anchor inside the heading.
      var space = Report();
      var block = InvestorBlock();
      var mark = RowContaining("IRR Details");

      var recipe = VerticalRepeat(On(mark).Heading("IRR Details").Of(block), separatedBy: BlankRows());
      var inside = VerticalRepeat(Heading("IRR Details").Of(On(mark).Of(block)), separatedBy: BlankRows());

      // One occurrence: the caption owns r1, the block runs to the first blank row, and the next
      // iteration finds no second "IRR Details" to anchor on and stops — quietly, which is the recipe.
      Assert.Equal(new[] { "Cash Flows Using Transfer Datex3" }, recipe.Map(space));

      var failure = Assert.Throws<ProjectionException>(() => inside.Map(space));

      // Loudly, and inside the FIRST occurrence: the caption has already consumed the anchor row, so
      // the content's own anchor finds nothing below it and the flow's placement — which always fits
      // — never gets the chance to end the repetition.
      Assert.Equal("VerticalRepeat[0] -> Heading -> Range#2", failure.Path);
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
        Observe(AfterBlankRows().AfterBlankColumns().Of(series), space),
        Observe(SkipEmptyRowsAndColumns().Of(series), space));
    }

    [Fact]
    public void AHoistedProjectionPlacedByOfReadsAsItsDeclaration()
    {
      // .Of places something already declared; a terminal declares it in place. Same words, same
      // reading — which is what lets a declaration move between the two spellings freely.
      var space = Report();
      var block = InvestorBlock();
      var mark = RowContaining(Transfer);

      AssertL3(
        Observe(On(mark).Of(VerticalRepeat(block, separatedBy: BlankRows())), space),
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
    private static ICellSpace Ledger() => Mixed(new object?[,]
    {
      { "Fund", "Amount" },
      { "Alpha", 100m },
      { "Beta", 250m },
    });

    private static IRowLandmark Header() => RowContaining("Fund");

    /// <summary>A bind pointed at the column of fund names, so every record fails.</summary>
    private static IProjectionDefinition<ICellSpace, decimal> FundColumnAsANumber(LabelMap captions) => Right(captions["Fund"]).Of(Decimal());

    [Fact]
    public void AVerticalRepeatTerminalKeepsTheIdentifierItsItemWasWrittenAs()
    {
      var investorDetail = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", throughPipeline.Path);
    }

    [Fact]
    public void AndSoDoesAHorizontalRepeatTerminal()
    {
      var quarterlyColumn = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).HorizontalRepeat(quarterlyColumn).Map(Ledger()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", throughPipeline.Path);
    }

    [Fact]
    public void AndATableRowSlotTerminal()
    {
      var allocationRow = Decimal();

      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", throughPipeline.Path);
    }

    [Fact]
    public void AndATableBindTerminal_LabelledByTheMethodGroupItWasPassedAs()
    {
      var throughPipeline = Assert.Throws<ProjectionException>(
        () => On(Header()).Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Ledger()));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", throughPipeline.Path);
    }

    // (Four more capture pins stood here, one per terminal on the SCOPED stage hierarchy — the
    // second family that carried a demand in its result type and therefore forwarded every
    // [CallerArgumentExpression] a second time. There is one stage hierarchy now, and it is the
    // generic one the four pins above are written through, so each of the four said the same thing
    // twice.)

    // --- 3. Heading is L3-by-construction ------------------------------------------------------------

    [Fact]
    public void AHeadingFailsInTheCaptionsOwnWords()
    {
      // A heading mints real Caption leaves, so a missing heading fails as the caption it is — the
      // subject and the sentence are the hand-written declaration's exactly, and the flow's own path
      // node is "Heading". This is what "structure, not a value" buys.
      var space = Report();

      var failure = Assert.Throws<ProjectionException>(() => Heading("Nope").Of(Series()).Map(space));

      Assert.Equal("Heading -> Caption(\"Nope\")#1", failure.Path);
      Assert.Equal("Caption(\"Nope\")#1", failure.Subject);
      Assert.Contains("no row containing 'Nope' exists", failure.Message);
      Assert.False(failure.IsFault);
    }

    [Fact]
    public void ChainedHeadingsAccumulateIntoOneFlowRatherThanNestedOnes()
    {
      // Two headings above one section are one statement. The path is where that is observable: one
      // Heading segment, two captions numbered within it.
      var space = Report();

      var failure = Assert.Throws<ProjectionException>(
        () => Heading("IRR Details").Heading("Nope").Of(Series()).Map(space));

      Assert.Equal(1, Occurrences(failure.Path, "Heading"));
      Assert.Equal("Heading -> Caption(\"Nope\")#2", failure.Path);
    }

    [Fact]
    public void WhereasLayeringIsNestingAndIsSpelledAsNesting()
    {
      // The negative pin, as the SPECIFIC difference: a heading over a heading-and-section is two
      // flows, and the second Heading in the path is where the reader sees it.
      var space = Report();

      var nested = Assert.Throws<ProjectionException>(
        () => Heading("IRR Details").Of(Heading("Nope").Of(Series())).Map(space));

      // Two Heading segments, and the inner one carries an ordinal because it is a CHILD of the outer
      // flow — which is precisely the difference: a chained heading is a sibling caption, a layered
      // one is a section inside a section.
      Assert.Equal(2, Occurrences(nested.Path, "Heading"));
      Assert.Equal("Heading -> Heading#2 -> Caption(\"Nope\")#1", nested.Path);
    }

    // --- 4. The construction guards ------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AHeadingCannotBeBlankOnAnyOfTheSurfacesThatMintOne(string? text)
    {
      // Null lands here rather than on ArgumentNullException deliberately: the question a heading
      // answers is "what does this section say", and no text is no text however it was spelled.
      foreach (var refusal in new[]
      {
        Assert.Throws<ArgumentException>(() => Heading(text!)),
        Assert.Throws<ArgumentException>(() => ProjectionBuilders<ICellSpace>.Heading(text!)),
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

      Assert.Equal(new[] { "Alphax2", "Beaconx1" }, pipeline.Map(space));
    }

    // The other half — a second position on one projection refused at construction — is now a
    // COMPILE-time refusal, since the postfix modifier that used to carry it is gone: On(a).On(b) does
    // not compile (the SecondAnchor stub), pinned in spike/PlacementGauntlet/MustNotCompilePipeline.cs
    // (Y). There is no runtime refusal left to reach, which is exactly what the pipeline above relies on.

    // --- 5. The teaching stubs -----------------------------------------------------------------------

    /// <summary>Every stage type, so a refusal cannot go missing from one of them.</summary>
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
        ["PlacementStage"] = (typeof(PlacementStage<ICellSpace>), Down(1)),
        ["UnboundedStage"] = (typeof(UnboundedStage<ICellSpace>), Down(1)),
        ["OffsetStage"] = (typeof(OffsetStage<ICellSpace>), Down(1)),
        ["OffsetAndSizeStage"] = (typeof(OffsetAndSizeStage<ICellSpace>), Down(1).Sized(WholeExtent())),
        ["BoundStage"] = (typeof(BoundStage<ICellSpace>), Until(RowContaining("IRR Details"))),
        ["HeadingStage"] = (typeof(HeadingStage<ICellSpace>), Heading("IRR Details")),
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
      // only a second anchor (5); a sized one refuses the movements as well (6, all of them — the
      // three of Down/Right/AfterBlank*, the new SkipToFirstNonBlankCell offset entry, and Sized
      // itself); a bounded one refuses a second end, an extent and the anchors (12); a headed one
      // refuses everything but another heading (13, gaining the same new offset entry).
      //
      // Every member that takes a landmark or a strategy is DOUBLED, taking the canonical form or
      // the typed phantom (`IRowLandmark<TSpace>`, `IAreaStrategy<TSpace>`, ...) that carries a
      // demand across the erased seam. A stage that refused only the canonical half of a doubled
      // member would refuse in the library's words down one overload and in the compiler's down the
      // other, for the same contradiction; so each refusal is spelled twice, once per twin. Nothing
      // about the taxonomy moved — only how many spellings each refusal has to cover: an unbounded
      // pipeline's five anchors become ten; a sized one adds the typed Sized to its six; a bounded
      // one doubles its five anchors and Sized (Until/UntilColumn were already doubled), 12 -> 18; a
      // headed one doubles the same five anchors, Sized, Until and UntilColumn, 13 -> 21.
      var counted = Stages.ToDictionary(stage => stage.Key, stage => Refusals(stage.Value.Type), StringComparer.Ordinal);

      Assert.Equal(
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
          ["PlacementStage"] = 0,
          ["UnboundedStage"] = 10,
          ["OffsetStage"] = 0,
          ["OffsetAndSizeStage"] = 7,
          ["BoundStage"] = 18,
          ["HeadingStage"] = 21,
        },
        counted);
    }

    /// <summary>The canonical form of a parameter that takes a demand, or the type unchanged.</summary>
    private static Type Erased(Type parameter)
    {
      if (!parameter.IsGenericType || parameter.Namespace != "Unrect.Projections")
        return parameter;

      var demanding = parameter.GetGenericTypeDefinition();

      if (demanding == typeof(IRowLandmark<>))
        return typeof(IRowLandmark);

      if (demanding == typeof(IColumnLandmark<>))
        return typeof(IColumnLandmark);

      if (demanding == typeof(Unrect.Projections.IAreaStrategy<>))
        return typeof(IAreaStrategy);

      if (demanding == typeof(Unrect.Projections.IOffsetStrategy<>))
        return typeof(IOffsetStrategy);

      return parameter;
    }

    [Theory]
    [MemberData(nameof(TheStageTypes))]
    public void AndATypedRefusalSaysWhatItsCanonicalTwinSays(string stage)
    {
      // The census counts spellings; this says the second spelling is the same refusal. A twin that
      // reached for a different one of the seven sentences would pass the census and the
      // library's-words law, and a declaration would be told two different reasons for one
      // contradiction depending on which overload it happened to bind.
      var (type, _) = Stages[stage];
      var twinned = 0;

      foreach (var member in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
      {
        var refusal = member.GetCustomAttribute<ObsoleteAttribute>();
        var parameters = member.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var canonical = parameters.Select(Erased).ToArray();

        if (refusal is null || canonical.SequenceEqual(parameters))
          continue;

        var twin = type.GetMethod(member.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, canonical, null);

        Assert.True(twin is not null, $"{stage}.{member.Name} refuses a demanding argument with no canonical twin beside it");

        var canonicalRefusal = twin!.GetCustomAttribute<ObsoleteAttribute>();

        Assert.True(canonicalRefusal is not null, $"{stage}.{member.Name} refuses the demanding form only");
        Assert.Equal(canonicalRefusal!.Message, refusal.Message);
        Assert.Equal(canonicalRefusal.IsError, refusal.IsError);

        twinned++;
      }

      // Non-vacuity: a stage whose typed refusals all disappeared would satisfy every assertion
      // above by having nothing to check, which is the failure the census is blind to from the
      // other side.
      Assert.Equal(TheTypedRefusals[stage], twinned);
    }

    /// <summary>How many of each stage's refusals are the demanding spelling of another.</summary>
    private static readonly IReadOnlyDictionary<string, int> TheTypedRefusals =
      new Dictionary<string, int>(StringComparer.Ordinal)
      {
        ["PlacementStage"] = 0,
        ["UnboundedStage"] = 5,
        ["OffsetStage"] = 0,
        ["OffsetAndSizeStage"] = 1,
        ["BoundStage"] = 8,
        ["HeadingStage"] = 8,
      };

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
