using System;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Presence — what kind of something, or of nothing, a projection made of its extent — pinned as
  /// the spec states it (<c>docs/design/presence-and-unit-spec.md</c>, DECIDED 2026-09-09). Four
  /// claims, in the order the spec makes them:
  /// <list type="number">
  /// <item>§4's classification table, one law per row: which spelling reports which presence.</item>
  /// <item>The composite join rule from the status header — <em>Read if any child Read, else Empty;
  /// Absorbed arises only at tolerance boundaries</em> — over both layouts.</item>
  /// <item>The repetition's exit reasons, which are the whole reason the enum exists: an item that
  /// was <see cref="Presence.Empty"/> and one that was <see cref="Presence.Absorbed"/> both end the
  /// run, and only the second is a declaration smell the repeat says anything about (D2).</item>
  /// <item>The compatibility law behind the amended D5 — <em>presence explains a stop, the extent
  /// decides one</em> — pinned on the shape that caught the earlier rule out: a tolerated item that
  /// still consumes its extent goes on repeating, and says nothing.</item>
  /// <item>The flow-unit law over the internal ε (D4): a unit child is deletable.</item>
  /// </list>
  /// <para>
  /// Presence is internal, so it is read through <c>Apply</c>'s result — the same value the engine,
  /// the layouts and the repeat guard act on. These are plain asserts rather than
  /// <see cref="Observations"/> comparisons because presence is not one of the observation's facets:
  /// it is metadata a caller cannot see, and the differential obligation (§6) is precisely that it
  /// changes none of the facets. The harness is used where the claim IS an equivalence — the unit
  /// law — and the negative pins there name the specific difference, per its one rule.
  /// </para>
  /// </summary>
  public class PresenceLawTests
  {
    // --- The grids, and the declarations that meet them --------------------------------------------

    /// <summary>A column of 1, 2, 3: a projection asking for text fails here, at a known cell.</summary>
    private static ISpace Numbers() => Ladder(3);

    /// <summary>Nothing but blank cells, so a discovered extent has somewhere to settle at zero.</summary>
    private static ISpace Blank() => Grid(new int[2, 2]);

    /// <summary>A value then a blank row: a flow can mix a child that reads with one that finds nothing.</summary>
    private static ISpace ValueThenNothing() => Grid(new[,] { { 1 }, { 0 } });

    /// <summary>Two names then a number, so a repeat of text cells finds two occurrences and then trouble.</summary>
    private static ISpace TwoNamesThenANumber() => Mixed(new object?[,] { { "a" }, { "b" }, { 1 } });

    /// <summary>A caption over two rows of content, for the anchored half of the table's last-but-one row.</summary>
    private static ISpace CaptionedSheet() => Mixed(new object?[,] { { "Detail" }, { 1 }, { 2 } });

    /// <summary>The discovered extent: full width, and as many leading rows as hold anything.</summary>
    private static IProjection<int> Rows() => Range(RowsWhileAnyValue(), b => b.Height);

    /// <summary>A cell read as text — which is a failure over <see cref="Numbers"/>, and an absorbable one.</summary>
    private static IProjection<string> Title() => Cell(v => v.GetString());

    /// <summary>The internal ε, at the one type these tests need it at.</summary>
    private static IProjection<int> Unit() => NothingProjection<int>.Instance;

    /// <summary>What the engine recorded about the reading — the value the composites act on.</summary>
    private static Presence PresenceOf<T>(IProjection<T> projection, ISpace space)
      => projection.Apply(space).Presence;

    // --- §4, the classification table, one law per row ---------------------------------------------

    [Fact]
    public void AProjectionThatReadContentIsRead()
    {
      // The default, and the one that needs no machinery: a leaf that read a cell, and a composite
      // over leaves that did.
      Assert.Equal(Presence.Read, PresenceOf(IntCell(), Numbers()));
      Assert.Equal(Presence.Read, PresenceOf(VerticalFlow(v => v.Next(IntCell()) + v.Next(IntCell())), Numbers()));
    }

    [Fact]
    public void ADiscoveredExtentThatSettledAtZeroRowsIsEmpty()
    {
      // Empty is read off the SETTLED extent rather than off the strategy, so the same declaration
      // is Read wherever the rows are there to be found. Without the control this test would pass
      // just as well if Range always said Empty.
      Assert.Equal(Presence.Empty, PresenceOf(Rows(), Blank()));
      Assert.Equal(Presence.Read, PresenceOf(Rows(), Numbers()));
    }

    [Fact]
    public void ARepetitionIsEmptyWhenItCollectedNothingAndReadWhenItCollectedSomething()
    {
      // A repetition says its own presence rather than leaving it to be inferred from the zero it
      // consumed: the data held none of these.
      Assert.Equal(Presence.Empty, PresenceOf(VerticalRepeat(Rows()), Blank()));
      Assert.Equal(Presence.Read, PresenceOf(VerticalRepeat(IntCell()), Numbers()));
    }

    [Fact]
    public void AToleranceBoundaryThatAbsorbedSaysAbsorbedRatherThanEmpty()
    {
      // The distinction the enum exists for. Neither of these looked at the region, so neither may
      // claim the region is empty — that is a fact about data nobody read.
      Assert.Equal(Presence.Absorbed, PresenceOf(Title().Optional(), Numbers()));
      Assert.Equal(Presence.Absorbed, PresenceOf(Title().Else("missing"), Numbers()));
    }

    [Fact]
    public void AFallbackThatRanReportsItsOwnPresenceAndNotTheBoundarys()
    {
      // A fallback is a projection like any other, so all three answers are reachable through one:
      // it read, it looked and found zero, or it was itself a boundary that declined to look.
      Assert.Equal(Presence.Read, PresenceOf(Title().Else(Cell(c => c.GetInt().ToString())), Numbers()));
      Assert.Equal(Presence.Empty, PresenceOf(Title().Else(Rows().Select(rows => rows + " rows")), Blank()));
      Assert.Equal(Presence.Absorbed, PresenceOf(Title().Else(Title().Else("missing")), Numbers()));
    }

    [Fact]
    public void ABoundaryWhoseInnerSucceededReportsTheInnersPresence()
    {
      // An inert boundary is transparent to presence in both directions: it neither invents Absorbed
      // over a reading nor loses the Empty its inner declaration discovered.
      Assert.Equal(Presence.Read, PresenceOf(Rows().Optional(), Numbers()));
      Assert.Equal(Presence.Empty, PresenceOf(Rows().Optional(), Blank()));
    }

    [Fact]
    public void CaptionsAnchoredContentAndTablesThatMatchedAreAllRead()
    {
      // The table's own row, in its three spellings: a caption that found its row, the content it
      // anchors, and a table that bound a header.
      Assert.Equal(Presence.Read, PresenceOf(Caption("Detail"), CaptionedSheet()));
      Assert.Equal(Presence.Read, PresenceOf(Rows().Under(Caption("Detail")), CaptionedSheet()));
      Assert.Equal(Presence.Read, PresenceOf(Table(), Mixed(new object?[,] { { "Amount" }, { 1 }, { 2 } })));
    }

    [Fact]
    public void ALayoutThatDeclaredNothingStaysAFaultRatherThanBecomingAPresence()
    {
      // The table's last row, and the reason the join rule never has to answer for an empty child
      // list: "described nothing" is not a kind of nothing, it is an error, and a fault at that —
      // no tolerance boundary may absorb a declaration with a hole in it.
      var failure = Assert.Throws<ProjectionException>(() => VerticalFlow<int>(_ => 0).Map(Numbers()));

      Assert.True(failure.IsFault);
      Assert.Equal("a flow must declare at least one projection; this one called Next zero times", Problem(failure));
    }

    // --- The join rule: Read if any child Read, else Empty -----------------------------------------

    [Fact]
    public void AFlowOfChildrenThatBothReadIsRead()
    {
      var flow = VerticalFlow(v => v.Next(IntCell()) + v.Next(IntCell()));

      Assert.Equal(Presence.Read, PresenceOf(flow, Numbers()));
    }

    [Fact]
    public void AFlowWhoseChildrenAllFoundNothingIsEmpty()
    {
      // Two discovered extents over a blank grid, each settling at zero. The flow looked — through
      // every child it declared — and what it found was nothing, which is exactly Empty.
      var flow = VerticalFlow(v => v.Next(Rows()) + v.Next(Rows()));

      Assert.Equal(Presence.Empty, PresenceOf(flow, Blank()));
    }

    [Fact]
    public void AFlowThatMixesAnEmptyChildWithOneThatReadIsRead()
    {
      // Read is the join's absorbing element: one child that read content makes the whole layout a
      // region that was read, whatever its neighbours found.
      var flow = VerticalFlow(v => v.Next(IntCell()) + v.Next(Rows()));

      Assert.Equal(Presence.Read, PresenceOf(flow, ValueThenNothing()));
    }

    [Fact]
    public void AnOverlayJoinsItsChildrenTheSameWayAFlowDoes()
    {
      // Same rule, different bookkeeping: an overlay's children all start from its origin, and the
      // join is still "did any of them read". The Read child here is an EXPLICIT 1x1 region over
      // blank cells — Read is about the declaration having read its extent, not about the cells in
      // it holding anything.
      var nothing = Overlay(o => o.Next(Rows()) + o.Next(Rows()));
      var something = Overlay(o => o.Next(Rows()) + o.Next(Range(1, 1, b => b.Height)));

      Assert.Equal(Presence.Empty, PresenceOf(nothing, Blank()));
      Assert.Equal(Presence.Read, PresenceOf(something, Blank()));
    }

    [Fact]
    public void AbsorbedIsNeverAJoinResult()
    {
      // The rule's third clause, pinned in the case that could have broken it: a layout whose only
      // child was absorbed reports Empty, not Absorbed. Only a tolerance boundary may say it did not
      // look, and the layout did look — through the child it declared. Stated as the implementation
      // behaves AND as the spec's status header rules; if these ever part company, the spec wins the
      // argument and this test is the place it gets had.
      var absorbedOnly = VerticalFlow(v => v.Next(Title().Optional()));
      var absorbedThenRead = VerticalFlow(v =>
      {
        v.Next(Title().Optional());

        return v.Next(IntCell());
      });

      Assert.Equal(Presence.Empty, PresenceOf(absorbedOnly, Numbers()));
      Assert.Equal(Presence.Read, PresenceOf(absorbedThenRead, Numbers()));
    }

    [Fact]
    public void AChoiceReportsTheWinningAlternativesPresence()
    {
      // Not a join — a choice has exactly one child that ran — but the same principle as a
      // fallback's: the alternative that matched is a projection like any other, and what it made of
      // the extent is what the choice made of it. One declaration, both answers, decided by the
      // sheet: over numbers the winner reads three rows, over blanks it looks and finds none.
      var choice = Choice(Title(), Rows().Select(rows => rows + " rows"));

      Assert.Equal(Presence.Read, PresenceOf(choice, Numbers()));
      Assert.Equal(Presence.Empty, PresenceOf(choice, Blank()));
    }

    // --- The repetition's exit reasons -------------------------------------------------------------

    /// <summary>
    /// D2's teaching note, verbatim. Written out here rather than referenced from the production
    /// constant on purpose: a message a user reads is pinned by quoting it, or the pin only says the
    /// code equals itself.
    /// </summary>
    private const string EndedByTolerance =
      "the repetition ended at occurrence [2]: the item's failure was absorbed by a tolerance "
      + "boundary, and a tolerated item cannot drive a repetition — drop the boundary, or declare "
      + "atLeast: 0 if an empty run is the concern";

    [Fact]
    public void ARepetitionThatRanOutOfContentSaysNothing()
    {
      // The ordinary ending, and the control for the two below: the space ran out, which is not a
      // declaration smell and gets no commentary.
      var result = VerticalRepeat(IntCell()).MapWithDiagnostics(Numbers());

      Assert.Equal(new[] { 1, 2, 3 }, result.Value);
      Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ARepetitionEndedByAToleratedItemSaysSoExactlyOnce()
    {
      // The documented trap, now guided. The third occurrence's failure was absorbed, the attempt
      // was rolled back with the warning that came from it — so this Info is the only thing left
      // saying anything happened, and it is the whole of what is said.
      //
      // BOTH HALVES ARE PRESENT HERE, and that is why the note fires: the item was Absorbed AND it
      // stood still, since a bare Optional consumes nothing to absorb into. Drop either half and the
      // note goes: ARepetitionThatRanOutOfContentSaysNothing is a standstill without tolerance, and
      // AnAbsorbedItemThatStillConsumedItsExtentGoesOnRepeating is tolerance without a standstill.
      var item = Title().Optional();

      var result = VerticalRepeat(item).MapWithDiagnostics(TwoNamesThenANumber());

      Assert.Equal(new[] { "a", "b" }, result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      var note = Assert.Single(result.Diagnostics, d => d.Message.Contains("absorbed"));

      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Equal("VerticalRepeat", note.Subject);
      Assert.Equal(EndedByTolerance, note.Message);
    }

    [Fact]
    public void ARepetitionEndedByAnEmptyItemSaysNothingBecauseEmptyIsNotAbsorbed()
    {
      // THE point of the enum, in one pair of tests. This run ends for the same arithmetic reason as
      // the one above — an item that consumed nothing — and says nothing, because the item looked
      // and the data held none of these. Under the old rule the two were one number and
      // indistinguishable.
      var result = VerticalRepeat(Rows()).MapWithDiagnostics(Blank());

      Assert.Empty(result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("absorbed"));
    }

    [Fact]
    public void AtLeastStillRejectsARunThatCollectedTooFew()
    {
      // Presence changes reasons and future capability, never current outcomes (D5/§6): the
      // occurrence count is what atLeast is about, and an ending explained is still an ending.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(Title().Optional(), atLeast: 1).Map(Numbers()));

      Assert.Equal("expected at least 1 occurrences but found 0", Problem(failure));
    }

    // --- The guard decides, presence only explains (the compatibility law) ------------------------
    //
    // The amended D5, and the reason this section exists at all: an earlier build of presence made
    // the continue rule "Read with progress", which ended a run at the first absorption even where
    // the item had consumed rows. That is a semantic change wearing a diagnostic's clothes — it
    // silently dropped occurrences a declaration used to collect — so the numeric productivity guard
    // is the sole decider again (RepeatProjection.cs:133) and presence only explains a stop it did
    // not cause. These are compatibility pins: what they assert is what the vocabulary did before
    // presence existed, and the point of pinning it is that nothing in the suite said so when it
    // changed.

    /// <summary>
    /// A tolerated occurrence that still consumes a row: the boundary absorbs the failure, and the
    /// declared extent on the boundary — resolved before it can catch anything — is consumed in full
    /// whatever the boundary made of it. One row per occurrence, whether the row read or was
    /// tolerated.
    /// </summary>
    private static IProjection<int> ToleratedRow() => Integer().Optional().Sized(Extent(1, 1));

    [Fact]
    public void AnAbsorbedItemThatStillConsumedItsExtentGoesOnRepeating()
    {
      // Three rows of text under a declaration expecting numbers: every occurrence absorbs, every
      // occurrence yields the filler, and the run goes all the way to the end of the sheet because
      // the extent — not the presence — is what moves the cursor.
      var tolerated = ToleratedRow();

      var result = VerticalRepeat(tolerated).MapWithDiagnostics(Mixed(new object?[,] { { "x" }, { "y" }, { "z" } }));

      Assert.Equal(new[] { 0, 0, 0 }, result.Value);

      // One Warning per absorption, all of them kept: nothing was rolled back, because no attempt
      // was discarded. And no D2 Info — the run did not end by tolerance, it ended by running out of
      // sheet, which is the ordinary ending.
      Assert.Equal(3, result.Diagnostics.Count);
      Assert.All(result.Diagnostics, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
      Assert.All(result.Diagnostics, d => Assert.Equal("'tolerated'", d.Subject));
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("absorbed"));

      // And the run is a region that was read: three occurrences collected, the whole column
      // consumed. Presence describes the repetition, not the mood of the items inside it.
      var applied = VerticalRepeat(tolerated).Apply(Mixed(new object?[,] { { "x" }, { "y" }, { "z" } }));

      Assert.Equal(3, applied.Consumed.Height);
      Assert.Equal(Presence.Read, applied.Presence);
    }

    [Fact]
    public void ATolerantRepetitionCollectsReadAndAbsorbedOccurrencesAlike()
    {
      // The mixed run, which is the shape a real sheet has: a good row, a malformed one, a good one.
      // The tolerated row contributes its filler in the middle of the list rather than truncating it
      // — the whole reason a declaration puts a boundary on a repeat's item.
      var tolerated = ToleratedRow();

      var result = VerticalRepeat(tolerated).MapWithDiagnostics(Mixed(new object?[,] { { 1 }, { "y" }, { 3 } }));

      Assert.Equal(new[] { 1, 0, 3 }, result.Value);

      var warning = Assert.Single(result.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
      Assert.Contains("expected Number", warning.Message);
    }

    [Fact]
    public void ThePaddedSpellingKeepsTheRunGoingForTheSameReason()
    {
      // The second route to a consuming absorption, and the pin that the law is about the extent
      // rather than about one modifier: padding is added outside the boundary, so an absorbed
      // occupant still occupies its padding. Two rows per occurrence — the inner reading contributes
      // nothing and the padding contributes the rest — so six rows make three occurrences.
      var padded = Integer().Optional().Padded(1);
      var sheet = Mixed(new object?[,]
      {
        { "a", "b", "c" },
        { "d", "e", "f" },
        { "g", "h", "i" },
        { "j", "k", "l" },
        { "m", "n", "o" },
        { "p", "q", "r" },
      });

      var result = VerticalRepeat(padded).MapWithDiagnostics(sheet);

      Assert.Equal(new[] { 0, 0, 0 }, result.Value);
      Assert.Equal(3, result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning));
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("absorbed"));

      // The lone Info is the sheet's third column, which this declaration never describes — a
      // statement about the space left over, and not about how the run ended.
      var note = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);

      Assert.Contains("were not described", note.Message);

      var applied = VerticalRepeat(padded).Apply(sheet);

      Assert.Equal(6, applied.Consumed.Height);
      Assert.Equal(Presence.Read, applied.Presence);
    }

    // --- The flow-unit law over ε -----------------------------------------------------------------

    [Fact]
    public void TheUnitAcceptsAnythingReadsNothingAndSaysEmpty()
    {
      // ε's own denotation, which is what makes it deletable: it never fails, it occupies no cells,
      // and it reports Empty rather than the Read a projection gets for saying nothing about itself
      // — so a repeat guard that keys on presence stops at it exactly as it stops at a region that
      // was there and held nothing.
      var applied = Unit().Apply(Numbers());

      Assert.Equal(0, applied.Value);
      Assert.Equal(0, applied.Consumed.Width);
      Assert.Equal(0, applied.Consumed.Height);
      Assert.Equal(Presence.Empty, applied.Presence);

      // Nothing of its own is noticed. The one Info is the root's report of the space the
      // declaration did not describe, which is a statement about the sheet and not about ε.
      var note = Assert.Single(Unit().MapWithDiagnostics(Numbers()).Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Contains("were not described", note.Message);
    }

    [Fact]
    public void TheUnitIsALeftIdentityForAFlow()
    {
      // ε · x ≡ x, and the pin is at L3: value, offset, consumed extent, advance, and the
      // diagnostics in order, subject and path included. The children are named by their use site,
      // which is why L3 reaches — see the negative pin below for the one thing the extra child does
      // move.
      var x = Rows();
      var plain = VerticalFlow(v => v.Next(x));
      var withUnit = VerticalFlow(v =>
      {
        v.Next(Unit());

        return v.Next(x);
      });

      AssertL3(Observe(plain, Numbers()), Observe(withUnit, Numbers()));

      // Again with a child that notices something, so the L3 half of the claim is not two empty
      // lists agreeing: the absorbed reading's warning is the same sentence about the same cell,
      // under the same path, on both sides of the equation.
      var tolerated = Title().Optional();

      AssertL3(
        Observe(VerticalFlow(v => v.Next(tolerated)), Numbers()),
        Observe(
          VerticalFlow(v =>
          {
            v.Next(Unit());

            return v.Next(tolerated);
          }),
          Numbers()));
    }

    [Fact]
    public void TheUnitIsARightIdentityForAFlow()
    {
      // x · ε ≡ x, the mirror. A unit taken after the cursor has moved slices the remainder it is
      // handed and adds nothing to the advance, so a following sibling — or an enclosing repeat —
      // steps by exactly what x consumed.
      var x = Rows();
      var plain = VerticalFlow(v => v.Next(x));
      var withUnit = VerticalFlow(v =>
      {
        var value = v.Next(x);

        v.Next(Unit());

        return value;
      });

      AssertL3(Observe(plain, Numbers()), Observe(withUnit, Numbers()));
    }

    [Fact]
    public void TheUnitShiftsAnInlineSiblingsOrdinalWhichIsWhereL3StopsHolding()
    {
      // The first of the two negative pins, and the reason the identity above is stated over
      // use-site-named children. A child written inline has no identifier to borrow and falls to the
      // naming ladder's last rung — its kind and its 1-based POSITION — which the extra child moves.
      var plain = VerticalFlow(v => v.Next(Cell(c => c.GetString())));
      var withUnit = VerticalFlow(v =>
      {
        v.Next(Unit());

        return v.Next(Cell(c => c.GetString()));
      });

      var one = Assert.Throws<ProjectionException>(() => plain.Map(Numbers()));
      var two = Assert.Throws<ProjectionException>(() => withUnit.Map(Numbers()));

      Assert.Equal("Cell#1", one.Subject);
      Assert.Equal("Cell#2", two.Subject);
    }

    [Fact]
    public void TheUnitIsNotAnIdentityForASiblingThatFailsWhereItStands()
    {
      // The second negative pin, and the sharper one: ε consumes nothing, so a child after it is a
      // child following a sibling that consumed nothing, and the flow's note fires. The note is
      // appended to the PROBLEM, so this divergence is at L1 — the level a tolerance boundary above
      // either spelling would act on — not at L3 where a naming difference would sit.
      var x = Title();
      var plain = VerticalFlow(v => v.Next(x));
      var withUnit = VerticalFlow(v =>
      {
        v.Next(Unit());

        return v.Next(x);
      });

      var one = Assert.Throws<ProjectionException>(() => plain.Map(Numbers()));
      var two = Assert.Throws<ProjectionException>(() => withUnit.Map(Numbers()));

      Assert.DoesNotContain("note:", one.Problem);
      Assert.EndsWith("; note: the preceding sibling consumed nothing at this position", two.Problem);
    }

    // --- The differential obligation ---------------------------------------------------------------

    private const string DiscoveredEmpty = "a discovered extent that settled at zero";
    private const string AbsorbedBoundary = "a boundary that absorbed";
    private const string MixedFlow = "a flow mixing an empty child with one that read";
    private const string NoOccurrences = "a repetition that collected nothing";

    [Theory]
    [InlineData(DiscoveredEmpty)]
    [InlineData(AbsorbedBoundary)]
    [InlineData(MixedFlow)]
    [InlineData(NoOccurrences)]
    public void PresenceIsTheSameWhetherExtentsAreDiscoveredOrMeasuredUpFront(string declaration)
    {
      // §6's obligation, the one that keeps presence honest as denotation rather than as an artefact
      // of evaluation order. The engine claims it by construction — Empty is read off the SETTLED
      // extent, which both forcing modes agree on — and a by-construction claim is exactly the kind
      // this campaign exists to hold to a test. The expected value travels with each case so the
      // theory cannot pass by reporting Read four times.
      var (expected, lazily, eagerly) = BothWays(declaration);

      Assert.Equal(expected, lazily);
      Assert.Equal(expected, eagerly);
    }

    private static (Presence Expected, Presence Lazily, Presence Eagerly) BothWays(string declaration)
    {
      switch (declaration)
      {
        case DiscoveredEmpty:
          return Compare(Rows(), Blank(), Presence.Empty);
        case AbsorbedBoundary:
          return Compare(Title().Optional(), Numbers(), Presence.Absorbed);
        case MixedFlow:
          return Compare(
            VerticalFlow(v => v.Next(IntCell()) + v.Next(Rows())),
            ValueThenNothing(),
            Presence.Read);
        case NoOccurrences:
          return Compare(VerticalRepeat(Rows()), Blank(), Presence.Empty);
        default:
          throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "No such declaration.");
      }
    }

    /// <summary>
    /// The same declaration read twice: once with its bounds discovered as they are consumed, once
    /// with every declared extent measured up front — <c>LazyDenotationTests</c>' idiom, asked here
    /// about the one thing that suite cannot see.
    /// </summary>
    private static (Presence Expected, Presence Lazily, Presence Eagerly) Compare<T>(
      IProjection<T> projection,
      ISpace space,
      Presence expected)
    {
      var lazily = PresenceOf(projection, space);

      using (ProjectionEngine.ForceEager())
        return (expected, lazily, PresenceOf(projection, space));
    }
  }
}
