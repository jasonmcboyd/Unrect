using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// v0.4 §14.4 asks whether a normal form for modifiers exists. This suite answers the half a
  /// normal form has to be built on: for an ordered pair of modifiers, does the order matter, and at
  /// which observation level does the difference first show? The findings are the test names.
  /// <para>
  /// <strong>The conjecture, and where it broke.</strong> Clone modifiers were expected to form a
  /// commuting record and wrappers an order-meaningful stack. Both halves hold with three
  /// exceptions, each pinned below:
  /// </para>
  /// <list type="number">
  /// <item>Clones commute only when they write DIFFERENT fields. Two writes to the offset, the area
  /// or the bound were last-wins, with the discarded one never evaluated — a missing landmark in a
  /// replaced anchor was never looked for. The owner judged that erasure a footgun on 2026-09-09
  /// (survey §5's DECIDED block), so those pairs are now refused at construction; the equivalences
  /// they used to read as are recorded in the comments of the pins that replaced them.</item>
  /// <item>The movements are not one commuting group. Explicit steps commute; a content-sensitive
  /// movement (<c>AfterBlankRows</c>) does not commute with anything that changes the space its
  /// predicate is evaluated over — including a movement on the OTHER axis.</item>
  /// <item>Wrappers are not uniformly order-meaningful. <c>Select</c> commutes with the pad and the
  /// bound at L3, and a clone crossing a wrapper is invisible at L2 — what a clone written outside a
  /// wrapper changes is WHICH LAYER it lands on, which is L3 for a name and the wrapper's own FRAME
  /// for an extent.</item>
  /// </list>
  /// <para>
  /// <strong>The one general congruence found, and the one a normal form may use.</strong> Moving a
  /// placement across a non-frame-shifting wrapper (<c>Optional</c>, <c>Else</c>, <c>Padded</c>,
  /// <c>Select</c>) preserves the value and the ADVANCE and only re-splits offset against consumed —
  /// so it is invisible to a parent flow and visible to <c>Apply</c>. It fails for the two wrappers
  /// that search their own extent for content (<c>Until</c>, <c>Heading</c>), and it fails whenever a
  /// tolerance boundary actually absorbs.
  /// </para>
  /// <para>
  /// <strong>Not duplicated here.</strong> <c>Optional</c>×<c>On</c> is
  /// <c>BoundaryProjectionTests.ABoundaryInsideTheAnchor_AbsorbsAMissingAnchor</c> and its twin;
  /// <c>Until</c>×<c>Sized</c> at L2 is <c>UntilProjectionTests.SizedAfterUntil_IsWhatTheParentSees</c>
  /// and its twin (this file adds the L1 half those do not reach); <c>Sized</c>×<c>Sized</c> refused
  /// is now a compile-time refusal in <c>spike/PlacementGauntlet/MustNotCompilePipeline.cs</c> (AB),
  /// and <c>Named</c>×<c>Named</c> last-wins is <c>PlacementTests.RepeatedNames_KeepOnlyTheLast</c>;
  /// <c>Down</c>×<c>Right</c> at L1 is
  /// <c>PlacementTests.CrossAxisModifiers_ComposeIntoADiagonalAnchor</c>; <c>OrBlank</c>×placement
  /// at value level is <c>OrBlankTests.ItCommutesWithPlacement</c>; the alternation operators'
  /// associativity and idempotence are <c>AlternationLawProbeTests</c>.
  /// </para>
  /// </summary>
  public class ModifierCongruenceTests
  {
    // --- The sheets --------------------------------------------------------------------------------

    /// <summary>Four rows, two columns, the landmark on row 3 (index 2).</summary>
    private static ISheetCells Rows() => Mixed(new object?[,]
    {
      { "a0", "a1" },
      { "b0", "b1" },
      { "Mark", "m1" },
      { "c0", "c1" },
    });

    /// <summary>The same sheet with the landmark on the FIRST row, so a bound leaves nothing.</summary>
    private static ISheetCells MarkFirst() => Mixed(new object?[,]
    {
      { "Mark", "a1" },
      { "b0", "b1" },
      { "c0", "c1" },
      { "d0", "d1" },
    });

    /// <summary>A wholly blank first row — the filler a content-sensitive movement steps over.</summary>
    private static ISheetCells BlankLead() => Mixed(new object?[,]
    {
      { null, null },
      { "b0", "b1" },
      { "Mark", "m1" },
      { "c0", "c1" },
    });

    /// <summary>
    /// A first row that is blank in column 1 and not in column 0. Sliced to column 1 its first row
    /// IS blank; unsliced it is not — which is what makes a column movement change what "blank row"
    /// means.
    /// </summary>
    private static ISheetCells Ragged() => Mixed(new object?[,]
    {
      { "a0", null },
      { "b0", "b1" },
      { "Mark", "m1" },
      { "c0", "c1" },
    });

    /// <summary>Column 1 is numbers all the way down, so a <c>Text</c> leaf fails wherever it lands.</summary>
    private static ISheetCells Numbers() => Mixed(new object?[,]
    {
      { 1, "a1" },
      { 2, "b1" },
      { 3, "m1" },
      { 4, "c1" },
    });

    private static IRowLandmark Mark() => RowContaining("Mark");

    private static IRowLandmark Missing() => RowContaining("Nope");

    /// <summary>A region that renders its own extent and contents, so every geometric difference shows.</summary>
    private static IProjection<ISheetCells, string> Block() => Range(block =>
    {
      var parts = new string[block.Width * block.Height];

      for (var row = 0; row < block.Height; row++)
        for (var column = 0; column < block.Width; column++)
          parts[(row * block.Width) + column] = Describe(block[column, row]);

      return $"({block.Width}x{block.Height}:{string.Join(",", parts)})";
    });

    private static string Describe(Point<ISheetCells> cell)
      => cell.IsBlank ? "_" : cell.IsText ? cell.Text() : cell.Describe();

    /// <summary>
    /// One modifier, by name — so a theory can name a pair rather than carry two lambdas. The
    /// geometry names are the pipeline entries hoisted through <c>.Of</c>, which is the only spelling
    /// of geometry now; <c>Entry.Of(x)</c> writes the same placement field the retired postfix
    /// modifier wrote, so <c>With(b, With(a, x))</c> still reads as "a inside, b outside".
    /// </summary>
    private static IProjection<ISheetCells, string> With(string modifier, IProjection<ISheetCells, string> projection) => modifier switch
    {
      "Named" => projection.Named("n"),
      "Sized" => Sized(Extent(2, 2)).Of(projection),
      "OffsetBy" => OffsetBy(SkipRows(1)).Of(projection),
      "On" => On(Mark()).Of(projection),
      "Below" => Below(Mark()).Of(projection),
      "Down" => Down(1).Of(projection),
      "Right" => Right(1).Of(projection),
      "Optional" => projection.Optional()!,
      "Else" => projection.Else("z"),
      "Until" => Until(Mark()).Of(projection),
      "Padded" => projection.Padded(0, 1, 0, 0),
      "Select" => projection.Select(value => value + "!"),
      "Heading" => Heading("Mark").Of(projection),
      _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, "No such modifier."),
    };

    // --- The clone record: different fields commute at L3 -------------------------------------------

    [Theory]
    [InlineData("Named", "Sized")]
    [InlineData("Named", "On")]
    [InlineData("Named", "Down")]
    [InlineData("Named", "OffsetBy")]
    [InlineData("Named", "Below")]
    [InlineData("Sized", "On")]
    [InlineData("Sized", "Down")]
    [InlineData("Sized", "OffsetBy")]
    public void TheCloneRecordCommutesAtL3WhereTwoModifiersWriteDifferentFields(string first, string second)
    {
      // A clone modifier copies the receiver and writes one field, so two of them that write
      // different fields land the same record whichever was written first — value, extent, offset,
      // diagnostics and all. This is the strongest congruence in the vocabulary and the reason the
      // name, the offset and the area can be treated as an unordered record by a normal form.
      var space = Rows();

      var forwards = Observe(With(second, With(first, Block())), space);
      var backwards = Observe(With(first, With(second, Block())), space);

      AssertL3(forwards, backwards);

      // Non-vacuity: a pair that failed both ways would agree at L3 while pinning nothing.
      Assert.Null(forwards.Failure);
    }

    [Theory]
    [InlineData("Named")]
    [InlineData("On")]
    [InlineData("Down")]
    [InlineData("Right")]
    public void OrBlankJoinsTheCloneRecordAtL3(string modifier)
    {
      // OrBlank rebuilds the leaf rather than wrapping it, carrying the placement and the name
      // across, so it behaves as a clone: it commutes with the rest of the record at L3.
      // OrBlankTests.ItCommutesWithPlacement pins the value and the extent for one pair; this is the
      // same claim at the level the law suite states things at, over the record.
      var space = Rows();

      AssertL3(
        Observe(With(modifier, Text()).OrBlank()!, space),
        Observe(With(modifier, Text().OrBlank()!), space));
    }

    [Fact]
    public void ExplicitMovementsCommuteAtL3()
    {
      // PlacementTests.CrossAxisModifiers_ComposeIntoADiagonalAnchor pins the value; the law holds
      // to L3. It is a law about EXPLICIT steps only — see the two tests below, which are why this
      // one is deliberately not called "movements commute".
      var space = Rows();

      AssertL3(
        Observe(Down(1).Right(1).Of(Block()), space),
        Observe(Right(1).Down(1).Of(Block()), space));

      AssertL3(
        Observe(Down(1).Down(2).Of(Block()), space),
        Observe(Down(2).Down(1).Of(Block()), space));
    }

    // --- Two writes to one field: refused where they are written --------------------------------------
    //
    // These four were the survey's loud hazard pins, and each was stated as an EQUIVALENCE to the
    // modifier-free form, because "the earlier one is discarded" is a weaker claim than "the
    // declaration reads as though it had never been written" — and the second was what was true,
    // including for a landmark that was not there to be found. The erasure was judged a footgun:
    // every reading a
    // reasonable person brings to x.On(a).Below(b) is spelled some other way — the sequential reading
    // is nesting, the conjunctive one belongs at the matcher level — so declared-over-declared has no
    // legitimate use, and a contradiction with no denotation gets no spelling. The equivalences below
    // are recorded in the comments; what is pinned is the refusal, at construction, before any space
    // exists for either landmark to be sought in.

    [Fact]
    public void AMovementComposesOntoAnAnchorRatherThanRestatingThePosition()
    {
      // Hazard 3, the runtime half of which is gone: Down(2).On(m) — an anchor after a movement — was
      // a construction-time ArgumentException while placement rode postfix modifiers, and is now a
      // COMPILE-time refusal (an anchor is an entry, not a stage), pinned in
      // spike/PlacementGauntlet/MustNotCompilePipeline.cs (Z). What survives, and is what the
      // vocabulary offers instead: a movement composes onto the anchor rather than answering the same
      // question a second time.
      Assert.Equal("c0", On(Mark()).Down(1).Of(Text()).Map(Rows()));
    }

    [Fact]
    public void AnAnchorOverAnAnchorIsRefusedSoNeitherLandmarkGoesUnsought()
    {
      // Hazard 1, the sharpest silent discard in the vocabulary. It was pinned as
      // .On(missing).Below(m) ≡L3 .Below(m): the replaced anchor was a strategy that was DROPPED,
      // not a search that failed, so a landmark existing nowhere in the file cost nothing. That
      // refusal is now a COMPILE-time one — a second anchor over the first is unspellable through the
      // pipeline (MustNotCompilePipeline.cs Y/AY) — so "never sought" is unreachable by construction.
      // What survives at run time: one anchor reads, and one missing landmark is still loud at Map.
      var space = Rows();

      Assert.Equal("c0", Below(Mark()).Of(Text()).Map(space));
      Assert.Contains(
        "no row containing 'Nope' exists in the available space",
        Assert.Throws<ProjectionException>(() => On(Missing()).Of(Text()).Map(space)).Message);
    }

    [Fact]
    public void EachAnchorAloneLandsWhereItAlwaysDid()
    {
      // Was TwoAnchorsAreLastWinsAndLandOnDifferentRows, then TwoAnchorsAreRefusedInEitherOrder: two
      // anchors on one section answered "where does this start?" twice, and the runtime refusal that
      // replaced the last-wins reading is now a compile-time one (MustNotCompilePipeline.cs Y). What
      // remains to pin is that each anchor alone lands where it always did, one row apart.
      var space = Rows();

      Assert.Equal("Mark", On(Mark()).Of(Text()).Map(space));
      Assert.Equal("c0", Below(Mark()).Of(Text()).Map(space));
    }

    [Fact]
    public void ASecondBoundIsRefusedSoNeitherLandmarkGoesUnsought()
    {
      // Hazard 2, the same discard one layer up. It was pinned as
      // .Until(missing).Until(m) ≡L3 .Until(m) — the replaced landmark never looked for, so a bound
      // that could not possibly be found was silently harmless while the reverse order failed
      // loudly. A projection has one end, and now says so where the second one is written.
      var space = Rows();

      // The chained spelling is now a compile-time refusal (BoundStage.Until, MustNotCompilePipeline.cs
      // AD); the runtime guard survives on the nested spelling, where the outer bound meets an
      // already-bounded projection and refuses to replace its end.
      var refused = Assert.Throws<ArgumentException>(() => Until(Mark()).Of(Until(Missing()).Of(Block())));

      Assert.Equal("projection", refused.ParamName);
      Assert.Contains("already ends at a landmark, and Until would replace that end", refused.Message);
      Assert.Contains("Bound it once", refused.Message);

      Assert.Equal(
        "projection",
        Assert.Throws<ArgumentException>(() => Until(Missing()).Of(Until(Mark()).Of(Block()))).ParamName);

      // Non-vacuity, as above: one bound reads, and a missing one is still loud at Map time.
      Assert.Equal("(2x2:a0,a1,b0,b1)", Until(Mark()).Of(Block()).Map(space));
      Assert.Contains(
        "no row containing 'Nope' exists to end this projection",
        Assert.Throws<ProjectionException>(() => Until(Missing()).Of(Block()).Map(space)).Message);
    }

    // --- The movements are not one commuting group ----------------------------------------------------

    [Fact]
    public void AContentSensitiveMovementDoesNotCommuteWithAMovementOnTheOtherAxis()
    {
      // The finding that stops "movements commute" from being a law. AfterBlankRows tests whole rows
      // of the space it is handed, and a column movement changes which space that is: after
      // .Right(1) the first row IS blank, so the movement steps over it; before .Right(1) the row
      // carries a value in column 0, so the movement does nothing and the leaf reads the blank cell
      // the other spelling stepped past.
      var space = Ragged();

      Assert.Equal("b1", Right(1).AfterBlankRows().Of(Text()).Map(space));

      var failure = Assert.Throws<ProjectionException>(() => AfterBlankRows().Right(1).Of(Text()).Map(space));

      Assert.Equal("expected Text at B1, found Blank", Problem(failure));
    }

    [Fact]
    public void AContentSensitiveMovementDoesNotCommuteWithAStepOnItsOwnAxisEither()
    {
      // "Past the filler, then one more" and "one more, then past the filler" are different
      // declarations, and only the first is what a reader of a gap-then-section sheet means.
      var space = BlankLead();

      Assert.Equal("Mark", AfterBlankRows().Down(1).Of(Text()).Map(space));
      Assert.Equal("b0", Down(1).AfterBlankRows().Of(Text()).Map(space));
    }

    // --- A clone crossing a wrapper: which layer it lands on -------------------------------------------

    [Theory]
    [InlineData("Optional")]
    [InlineData("Else")]
    [InlineData("Until")]
    [InlineData("Padded")]
    [InlineData("Select")]
    [InlineData("Heading")]
    public void NamingCommutesWithEveryWrapperAtL2AndNoHigher(string wrapper)
    {
      // A name is never geometry, so it cannot move a reading whichever layer it is written on. That
      // is the whole of the congruence: the level above is where the two spellings part, because the
      // name lands on a DIFFERENT NODE.
      var space = Rows();

      AssertL2(
        Observe(With(wrapper, Block().Named("n")), space),
        Observe(With(wrapper, Block()).Named("n"), space));
    }

    [Fact]
    public void ANameWrittenOutsideAWrapperNamesTheWrapperAndDeepensThePath()
    {
      // An unnamed wrapper is transparent and contributes no path segment; naming it makes it opaque
      // and it claims one. So the two spellings do not merely name different nodes — one of them
      // adds a level to the tree, and the thing the reader called 'n' is not the thing the warning
      // is about.
      var space = Numbers();

      var inside = Assert.Single(Text().Named("n").Optional().MapWithDiagnostics(space).Diagnostics);
      var outside = Assert.Single(Text().Optional().Named("n").MapWithDiagnostics(space).Diagnostics);

      Assert.Equal("'n'", inside.Subject);
      Assert.Equal("'n' (Text)", inside.Path);

      Assert.Equal("Text", outside.Subject);
      Assert.Equal("'n' -> Text", outside.Path);
    }

    [Fact]
    public void ANameWrittenOutsideABoundNamesTheBoundAndBothSpellingsStillBlameTheSection()
    {
      // The bound is a wrapper too, and the same rule decides which node the name lands on. What is
      // NOT the same: a missing bound is blamed on the projection being bounded either way, so here
      // only the path moves — the subject does not.
      // (UntilProjectionTests.ANamedBoundSpeaksForItself pins the outer spelling alone.)
      var space = Rows();

      var inside = Assert.Throws<ProjectionException>(() => Until(Missing()).Of(Text().Named("n")).Map(space));
      var outside = Assert.Throws<ProjectionException>(() => Until(Missing()).Of(Text()).Named("n").Map(space));

      Assert.Equal("'n'", inside.Subject);
      Assert.Equal("'n' (Text)", inside.Path);

      Assert.Equal("'n'", outside.Subject);
      Assert.Equal("'n' (Until)", outside.Path);
    }

    [Fact]
    public void ANameWrittenInsideAPadNeverReachesThePaddingsOwnFailure()
    {
      // A pad that does not fit blames itself explicitly, so the name has to be on the PAD to appear
      // at all. Written on the inner projection it is not merely in the wrong place — it is absent
      // from the message a reader gets.
      var space = Rows();

      var inside = Assert.Throws<ProjectionException>(() => Text().Named("n").Padded(3).Map(space));
      var outside = Assert.Throws<ProjectionException>(() => Text().Padded(3).Named("n").Map(space));

      Assert.Equal("Padded", inside.Subject);
      Assert.Equal("Padded", inside.Path);

      Assert.Equal("'n'", outside.Subject);
      Assert.Equal("'n' (Padded)", outside.Path);
    }

    [Fact]
    public void AnExtentDeclaredOutsideABoundIsTheFrameTheLandmarkIsSoughtIn()
    {
      // TEST-91 pins Until×Sized as an L2 difference: the modifier written last is what the parent
      // consumes. The difference reaches L1, which is the sharper hazard: a .Sized written outside
      // the bound becomes the region the landmark is searched in, so a landmark past the declared
      // extent is not found at all and a working declaration becomes a hard failure.
      var space = Rows();

      Assert.Equal("(2x2:a0,a1,b0,b1)", Sized(Extent(2, 2)).Until(Mark()).Of(Block()).Map(space));

      var failure = Assert.Throws<ProjectionException>(() => Sized(Extent(2, 2)).Of(Until(Mark()).Of(Block())).Map(space));

      Assert.Equal("no row containing 'Mark' exists to end this projection", Problem(failure));
    }

    [Fact]
    public void AnExtentDeclaredOutsideAHeadingIsTheFrameTheCaptionIsSoughtIn()
    {
      // The same law for the other content-searching wrapper. Heading is a flow, and a flow's
      // declared extent is what its first child — the caption — gets to look in.
      var space = Rows();

      Assert.Equal("(2x1:c0,c1)", Heading("Mark").Of(Sized(Extent(2, 1)).Of(Block())).Map(space));

      var failure = Assert.Throws<ProjectionException>(() => Sized(Extent(2, 1)).Of(Heading("Mark").Of(Block())).Map(space));

      Assert.Contains("no row containing 'Mark' exists in the available space", failure.Message);
    }

    [Theory]
    [InlineData("Optional")]
    [InlineData("Else")]
    [InlineData("Padded")]
    [InlineData("Select")]
    public void AMovementCrossingAWrapperKeepsTheAdvanceAndResplitsTheOffset(string wrapper)
    {
      // The one general congruence a normal form may use, and its exact bound. A placement written
      // outside a wrapper is resolved by the engine before the wrapper runs; written inside, the
      // wrapper's own extent absorbs it. The reading and the ADVANCE — everything a parent flow
      // sees — are identical; how the advance is split between offset and consumed is not.
      // TEST-49 records this for Select as "value + advance"; it is true of the whole family, and
      // false of Until and Under, which search the extent the offset moved.
      var space = Rows();

      var inside = Observe(With(wrapper, Down(1).Of(Block())), space);
      var outside = Observe(Down(1).Of(With(wrapper, Block())), space);

      AssertL1(inside, outside);
      Assert.Equal(inside.Advance, outside.Advance);

      Assert.Equal("0x0", inside.Offset);
      Assert.Equal("0x1", outside.Offset);
      Assert.NotEqual(inside.Consumed, outside.Consumed);
    }

    // --- A tolerance boundary is a scope, and geometry written outside it survives absorption ----------
    //
    // Four faces of one rule. What a boundary absorbs it also erases the extent of: an absorbed
    // reading consumes nothing, because nothing was read and no honest extent exists. Every
    // geometric modifier written OUTSIDE the boundary still applies — it was resolved before the
    // boundary ran, or is applied to what the boundary returned. So the order decides whether the
    // parent's cursor moves at all.

    [Fact]
    public void AnExtentDeclaredOutsideABoundaryIsConsumedThoughNothingWasRead()
    {
      var space = Numbers();

      var inside = Observe(Sized(Extent(1, 1)).Of(Text()).Optional()!, space);
      var outside = Observe(Sized(Extent(1, 1)).Of(Text().Optional())!, space);

      AssertL1(inside, outside);

      Assert.Equal("0x0", inside.Consumed);
      Assert.Equal("1x1", outside.Consumed);
    }

    [Fact]
    public void APadWrittenOutsideABoundaryOutlivesTheAbsorption()
    {
      var space = Numbers();

      var inside = Observe(Text().Padded(0, 1, 0, 0).Optional()!, space);
      var outside = Observe(Text().Optional().Padded(0, 1, 0, 0)!, space);

      AssertL1(inside, outside);

      Assert.Equal("0x0", inside.Consumed);
      Assert.Equal("0x1", outside.Consumed);
    }

    [Fact]
    public void AMovementWrittenOutsideABoundaryStillAdvancesThoughNothingWasRead()
    {
      // The one place where the advance-preserving congruence above stops: once the boundary
      // absorbs, the two spellings disagree about the advance itself, so a following sibling starts
      // in a different place.
      var space = Numbers();

      var inside = Observe(Down(1).Of(Text()).Optional()!, space);
      var outside = Observe(Down(1).Of(Text().Optional())!, space);

      AssertL1(inside, outside);

      Assert.Equal("0x0", inside.Advance);
      Assert.Equal("0x1", outside.Advance);
    }

    [Fact]
    public void ABoundWrittenOutsideABoundaryIsNotAbsorbed()
    {
      // The Until twin of TEST-78, and it is not pinned anywhere else in either direction as a pair.
      // A boundary's own placement resolves before it can catch anything — and so does every wrapper
      // written outside it, the bound included.
      var space = Rows();

      Assert.Null(Until(Missing()).Of(Text()).Optional().Map(space));

      var failure = Assert.Throws<ProjectionException>(() => Until(Missing()).Of(Text().Optional()).Map(space));

      Assert.Equal("no row containing 'Nope' exists to end this projection", Problem(failure));
    }

    [Fact]
    public void ACaptionSoughtOutsideABoundaryIsNotAbsorbedEither()
    {
      // Heading is sugar for a flow, and a flow written outside the boundary puts its caption search
      // outside too. So "the section may be absent" has to be written outside the Heading, not inside.
      var space = Rows();

      Assert.Null(Heading("Nope").Of(Text()).Optional().Map(space));

      var failure = Assert.Throws<ProjectionException>(() => Heading("Nope").Of(Text().Optional()).Map(space));

      Assert.Contains("no row containing 'Nope' exists in the available space", failure.Message);
    }

    [Fact]
    public void ASelectWrittenOutsideABoundaryTransformsTheStandIn()
    {
      // Select is neutral about placement and emphatically not about tolerance: outside the
      // boundary it is applied to the filler, which for Optional is the default value. A conversion
      // written on the wrong side of a tolerance boundary therefore runs on a value the document
      // never contained.
      var space = Numbers();

      Assert.Null(Text().Select(value => value + "!").Optional().Map(space));
      Assert.Equal("!", Text().Optional().Select(value => value + "!").Map(space));

      Assert.Equal("z", Text().Select(value => value + "!").Else("z").Map(space));
      Assert.Equal("z!", Text().Else("z").Select(value => value + "!").Map(space));
    }

    [Fact]
    public void APlacementWrittenOutsideABoundaryMovesTheStandInToo()
    {
      // The fallback is applied to the boundary's OWN extent, so a placement written outside the
      // boundary is shared by the primary and the stand-in, while one written inside belongs to the
      // primary alone. Both spellings read the same primary cell and disagree about where the
      // fallback then looks — which is the quietest of the order hazards, because both succeed.
      var space = BlankLead();
      var fallback = Down(1).Of(Text()).Named("fb");

      Assert.Equal("b0", Right(1).Of(Text()).Else(fallback).Map(space));
      Assert.Equal("b1", Right(1).Of(Text().Else(fallback)).Map(space));
    }

    [Fact]
    public void TwoTolerancesInEitherOrderDisagreeAboutWhichFillerWins()
    {
      // The outer boundary sees the inner one SUCCEED — absorbing is not failing — so the inner
      // filler is the answer and the outer one is unreachable. Which filler that is depends entirely
      // on the order.
      var space = Numbers();

      Assert.Null(Text().Optional().Else("z").Map(space));
      Assert.Equal("z", Text().Else("z").Optional().Map(space));
    }

    // --- The wrapper stack ------------------------------------------------------------------------------

    [Fact]
    public void AWrapperBetweenTwoBoundsIsTheDifferenceBetweenARefusalAndANesting()
    {
      // §6.6 calls .Until "context-sensitive replacement" and gives Select as the separator that
      // makes two bounds nest. The criterion is sharper than that: the receiver being the bound
      // wrapper ITSELF is what decides, so a clone modifier in between is invisible and ANY wrapper
      // in between — even a zero-cell pad — is a layer and leaves both bounds in force.
      //
      // Until 2026-09-09 the two halves were two different READINGS of the same declaration, and the
      // clone-separated half silently dropped a landmark it never sought — the survey's hazard 2.
      // They are now two different OUTCOMES: the clone-separated lines are refused where they are
      // written, the wrapper-separated ones nest exactly as before. The context-sensitivity is
      // therefore still real and no longer quiet.
      var space = Rows();

      // Clones do not separate, so all three state two ends for one projection — the second bound
      // still meets the bound wrapper itself, whichever clone sits between them.
      Assert.Throws<ArgumentException>(() => Until(Mark()).Of(Until(Missing()).Of(Block())));
      Assert.Throws<ArgumentException>(() => Until(Mark()).Of(Until(Missing()).Of(Block()).Named("n")));
      Assert.Throws<ArgumentException>(() => Until(Mark()).Of(Down(0).Of(Until(Missing()).Of(Block()))));

      // Wrappers do: the inner bound is still in force, and still fails — at Map, not where written.
      Assert.Equal(
        "no row containing 'Nope' exists to end this projection",
        Problem(Assert.Throws<ProjectionException>(() => Until(Mark()).Of(Until(Missing()).Of(Block()).Padded(0)).Map(space))));

      Assert.Equal(
        "no row containing 'Nope' exists to end this projection",
        Problem(Assert.Throws<ProjectionException>(() => Until(Mark()).Of(Until(Missing()).Of(Block()).Select(value => value)).Map(space))));

      // Non-vacuity for the nesting half: two bounds that both exist both apply, the outer one
      // framing the search for the inner. The outer leaves rows 0-1; the inner stops before "b0".
      Assert.Equal(
        "(2x1:a0,a1)",
        Until(Mark()).Of(Until(RowContaining("b0")).Of(Block()).Select(value => value)).Map(space));
    }

    [Fact]
    public void AWrapperBetweenTwoPlacementsIsTheDifferenceBetweenARefusalAndANestingToo()
    {
      // The offset and the extent families' half of the test above, and the thing the refusal's own
      // teaching sentence points at: "place the region and place this projection within it". A
      // wrapper is built with Placement.Default, so a placement written outside one lands on the
      // WRAPPER and the receiver keeps its own — two placements, on two projections, neither
      // erased. That is why the refusal can be as blunt as it is, and pinning it here is what stops
      // a future tightening of the check from walking through wrappers and taking the escape hatch
      // with it.
      var twice = Mixed(new object?[,]
      {
        { "x", "top" },
        { "b0", "b1" },
        { "Mark", "m1" },
        { "x", "bottom" },
      });

      // Adjacent, the two anchors are a contradiction — now a compile-time refusal
      // (MustNotCompilePipeline.cs Y), where it used to be a runtime one...

      // ...and through a wrapper they are a search inside a search: the outer anchor frames rows
      // 2-3, and the inner one finds the SECOND "x" in what is left. Were the outer one dropped the
      // reading would be "top"; were the inner one dropped it would be "m1".
      Assert.Equal(
        "bottom",
        On(Mark()).Of(On(RowContaining("x")).Of(Row(strip => strip[1].Text())).Select(value => value)).Map(twice));

      // The extent family says the same thing one field over, and needs Apply to show it: the inner
      // extent is what the block reads, the outer is what the parent steps past. Adjacent, two extents
      // are a compile-time refusal too (MustNotCompilePipeline.cs AB); through a wrapper they nest.
      var applied = Sized(Extent(2, 2)).Of(Sized(Extent(1, 1)).Of(Block()).Padded(0)).Apply(Rows());

      Assert.Equal("(1x1:a0)", applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void ABoundAndAPadDoNotCommuteWhenTheLandmarkSitsInThePadding()
    {
      // Both wrappers rewrite the extent, so whichever is outermost decides what the other one sees:
      // a pad outside the bound hides the landmark row from the search, a bound outside the pad
      // leaves the pad nothing to inset. Two failures, and neither declaration is the other.
      var space = MarkFirst();

      var boundOutside = Assert.Throws<ProjectionException>(() => Until(Mark()).Of(Block().Padded(0, 1, 0, 0)).Map(space));
      var padOutside = Assert.Throws<ProjectionException>(() => Until(Mark()).Of(Block()).Padded(0, 1, 0, 0).Map(space));

      Assert.Equal(
        "a padding of 0 left, 1 top, 0 right, 0 bottom does not fit an extent of 2x0",
        Problem(boundOutside));

      Assert.Equal("no row containing 'Mark' exists to end this projection", Problem(padOutside));
      Assert.Equal("A2", padOutside.Location.A1);
    }

    [Fact]
    public void PaddingNestsSoTwoPadsAreTheSumOfTheirInsets()
    {
      // The nesting rule stated as an order-independent one: two pads add, and adding commutes, so
      // this is one of the few wrapper pairs that genuinely commutes at L3.
      var space = Rows();

      Assert.Equal("(2x2:Mark,m1,c0,c1)", Block().Padded(0, 1, 0, 0).Padded(0, 1, 0, 0).Map(space));

      AssertL3(
        Observe(Block().Padded(0, 1, 0, 0).Padded(1, 0, 0, 0), space),
        Observe(Block().Padded(1, 0, 0, 0).Padded(0, 1, 0, 0), space));
    }

    [Theory]
    [InlineData("Padded")]
    [InlineData("Until")]
    [InlineData("Heading")]
    [InlineData("Optional")]
    public void SelectCommutesWithEveryOtherWrapperAtL3WhileNothingIsAbsorbed(string wrapper)
    {
      // Select changes the value and nothing else, so among the wrappers it is the one that stacks
      // in any order — as long as no boundary under it exercises itself, which is the case the test
      // above owns.
      var space = Rows();

      AssertL3(
        Observe(With(wrapper, Block()).Select(value => value + "!"), space),
        Observe(With(wrapper, Block().Select(value => value + "!")), space));
    }

    [Fact]
    public void TheToleranceBoundaryAndThePadCommuteAtL3WhileNothingIsAbsorbed()
    {
      // The positive half of the boundary-scope rule: while the inner projection succeeds, a
      // boundary is inert and a pad written on either side of it is the same declaration.
      var space = Rows();

      AssertL3(
        Observe(Block().Padded(0, 1, 0, 0).Optional()!, space),
        Observe(Block().Optional()!.Padded(0, 1, 0, 0), space));
    }

    // --- Composability is itself a table entry ---------------------------------------------------------

    [Theory]
    [InlineData("Optional")]
    [InlineData("Until")]
    [InlineData("Padded")]
    [InlineData("Select")]
    public void OrBlankRefusesEveryWrapperAtConstructionTimeNamingTheOneItRefused(string wrapper)
    {
      // The one pair in the table that does not compose in one direction and does in the other, and
      // it is refused at construction rather than by the compiler: OrBlank belongs to a leaf that
      // declared a kind, and a wrapper is not one. Written the other way round — OrBlank first, the
      // wrapper after — every one of these composes, which is why the refusal has to name the
      // receiver rather than merely say no.
      var failure = Assert.Throws<ArgumentException>(() => With(wrapper, Text()).OrBlank());

      Assert.Equal("projection", failure.ParamName);
      Assert.Contains("OrBlank reads a blank cell as null", failure.Message);
      Assert.Contains(" is not one.", failure.Message);

      // ...and the composable direction really is composable.
      Assert.NotNull(With(wrapper, Text().OrBlank()!));
    }

    // A congruence pin stood here for `Demanding` — the ascription that stated a demand in the
    // static type and handed back the SAME OBJECT, so there was nothing for an order to change. Both
    // the modifier and the witness it took are deleted: a declaration is typed by the space its file
    // named, so there is nothing left to ascribe.
  }
}
