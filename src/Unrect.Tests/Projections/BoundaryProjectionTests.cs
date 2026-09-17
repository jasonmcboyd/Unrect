using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>Else</c> and <c>Optional</c> declare tolerance at the one projection where it is
  /// acceptable. They behave like a catch block: everything underneath still fails exactly as
  /// loudly, the failure travels up to the nearest boundary, and the boundary records what actually
  /// went wrong before supplying a filler. There is no lenient mode to switch on.
  /// </summary>
  public class BoundaryProjectionTests
  {
    // One column of numbers, so a projection asking for text is a guaranteed, well-located failure.
    private static ISheetCells Numbers(int height = 3)
    {
      var values = new int[height, 1];

      for (var row = 0; row < height; row++)
        values[row, 0] = row + 1;

      return Grid(values);
    }

    private static IProjection<ISheetCells, string> Title() => TextCell().Named("title");

    /// <summary>Two levels below the boundary: the flow whose second child is the one that fails.</summary>
    private static IProjection<ISheetCells, string> Inner()
      => VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(TextCell())}");

    /// <summary>
    /// The one warning a parse produced. An absorbing boundary consumes nothing, so a boundary at
    /// the root also leaves an unconsumed-space Info covering the whole sheet; the warning is the
    /// part these tests are about.
    /// </summary>
    private static ProjectionDiagnostic Warning<T>(MapResult<T> result)
      => Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

    // --- What a boundary yields -----------------------------------------------------------------------

    [Fact]
    public void Optional_YieldsTheDefaultWhenTheProjectionFails()
    {
      Assert.Null(Title().Optional().Map(Numbers()));
    }

    [Fact]
    public void Optional_OnAValueType_YieldsThatTypesDefault()
    {
      // Not null — the filler for an int is 0. Where "absent" and "zero" must differ, Else(value)
      // or a projection to a nullable says so explicitly.
      Assert.Equal(0, TextCell().Select(text => text.Length).Optional().Map(Numbers()));
    }

    [Fact]
    public void ElseValue_YieldsTheConstantWhenTheProjectionFails()
    {
      Assert.Equal("missing", Title().Else("missing").Map(Numbers()));
    }

    [Fact]
    public void ElseProjection_YieldsTheFallbacksReadingWhenTheProjectionFails()
    {
      Assert.Equal("1", Title().Else(Point().Select(p => p.Integer().ToString()).Named("plan B")).Map(Numbers()));
    }

    [Fact]
    public void ABoundaryIsInertWhenTheProjectionSucceeds()
    {
      var space = Mixed(new object?[,] { { "Acme" } });

      var result = TextCell().Named("title").Optional().MapWithDiagnostics(space);

      Assert.Equal("Acme", result.Value);
      Assert.Empty(result.Diagnostics);
    }

    // --- What a boundary reports ------------------------------------------------------------------------

    [Fact]
    public void AnAbsorbedFailure_IsReportedAsAWarning()
    {
      // Info is for things going as designed; tolerance being exercised means the file was not
      // what the projection says it should be.
      var diagnostics = Title().Optional().MapWithDiagnostics(Numbers(1)).Diagnostics;

      Assert.Single(diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void AnAbsorbedFailure_IsDescribedByTheProjectionThatFailedNotTheBoundary()
    {
      // The boundary caught it; the leaf caused it. A warning is only actionable if it names the
      // latter.
      var warning = Warning(Title().Optional().MapWithDiagnostics(Numbers(1)));

      Assert.Equal("'title'", warning.Subject);
      Assert.Equal("'title' (Text)", warning.Path);
      Assert.Contains("expected Text at A1, found Number", warning.Message);
      Assert.Equal("A1", warning.Location.A1);
    }

    [Fact]
    public void EveryBoundarySpellingReportsTheSameFailure()
    {
      var optional = Warning(Title().Optional().MapWithDiagnostics(Numbers(1)));
      var elseValue = Warning(Title().Else("x").MapWithDiagnostics(Numbers(1)));
      var elseProjection = Warning(Title().Else(Point().Select(_ => "y").Named("plan B")).MapWithDiagnostics(Numbers(1)));

      Assert.Equal(optional.Message, elseValue.Message);
      Assert.Equal(optional.Message, elseProjection.Message);
      Assert.Equal("'title' (Text)", elseProjection.Path);
      Assert.All(
        new[] { optional, elseValue, elseProjection },
        d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
    }

    // --- What a boundary consumes -------------------------------------------------------------------------

    [Fact]
    public void AnAbsorbingBoundaryConsumesNothing()
    {
      // Nothing was read, so no honest extent exists. A following sibling starts where the failed
      // projection began rather than after it — which is why absorbing boundaries want
      // seek-anchored siblings rather than arithmetic.
      var applied = Title().Optional().Apply(Numbers());

      Assert.Equal(0, applied.Consumed.Width);
      Assert.Equal(0, applied.Consumed.Height);
    }

    [Fact]
    public void AFollowingSiblingStartsWhereTheAbsorbedProjectionBegan()
    {
      var read = VerticalFlow(v =>
        $"{v.Next(Title().Optional()) ?? "null"}|{v.Next(IntCell())}|{v.Next(IntCell())}")
        .Map(Numbers());

      Assert.Equal("null|1|2", read);
    }

    [Fact]
    public void ElseValue_AlsoConsumesNothing()
    {
      var read = VerticalFlow(v => $"{v.Next(Title().Else("missing"))}|{v.Next(IntCell())}").Map(Numbers());

      Assert.Equal("missing|1", read);
    }

    [Fact]
    public void ElseProjection_ConsumesWhateverTheFallbackConsumed()
    {
      // A fallback projection did read something, so it reports an honest extent and the next
      // sibling clears it.
      var read = VerticalFlow(v =>
        $"{v.Next(Title().Else(Point().Select(p => p.Integer().ToString()).Named("plan B")))}|{v.Next(IntCell())}")
        .Map(Numbers());

      Assert.Equal("1|2", read);
    }

    [Fact]
    public void ElseProjection_ReportsTheFallbacksAdvance()
    {
      var applied = Range(b => b.Width).Select(w => "wide").Else(Range(1, 2, b => "narrow")).Apply(Numbers());

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    // --- Depth ------------------------------------------------------------------------------------------------

    [Fact]
    public void ABoundaryAbsorbsAFailureFromDeepInsideIt()
    {
      // Three levels down: the boundary wraps a flow whose second child fails. Nothing between
      // them softens anything — the failure travels to the nearest boundary and stops there.
      var projection = VerticalFlow(v =>
        $"{v.Next(IntCell())}|{v.Next(Inner().Optional()) ?? "null"}|{v.Next(IntCell())}");

      var result = projection.MapWithDiagnostics(Numbers());

      Assert.Equal("1|null|2", result.Value);
    }

    [Fact]
    public void ADeeplyAbsorbedFailure_KeepsItsFullPathAndTrueLocation()
    {
      var projection = VerticalFlow(v =>
        $"{v.Next(IntCell())}|{v.Next(Inner().Optional()) ?? "null"}|{v.Next(IntCell())}");

      var warning = Assert.Single(
        projection.MapWithDiagnostics(Numbers()).Diagnostics,
        d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("VerticalFlow -> VerticalFlow#2 -> Text#2", warning.Path);
      Assert.Equal("A3", warning.Location.A1);
    }

    // --- Where the boundary sits decides what it can catch ---------------------------------------------------------

    [Fact]
    public void ABoundaryInsideTheAnchor_AbsorbsAMissingAnchor()
    {
      // The boundary's own placement resolves before it can catch anything, so an offset written
      // inside it is inside the try block.
      var space = Mixed(new object?[,] { { "nothing" }, { "here" } });

      var result = On(RowContaining("Section")).Of(TextCell()).Optional().MapWithDiagnostics(space);

      Assert.Null(result.Value);
      Assert.Contains(
        result.Diagnostics,
        d => d.Severity == DiagnosticSeverity.Warning && d.Message.Contains("no row containing 'Section'"));
    }

    [Fact]
    public void ABoundaryOutsideTheAnchor_DoesNotAbsorbAMissingAnchor()
    {
      // ...and an offset written outside it resolves first, so the anchor miss escapes. This is
      // what a Repeat wants: running out of anchors is how it knows to stop.
      var space = Mixed(new object?[,] { { "nothing" }, { "here" } });

      var failure = Assert.Throws<ProjectionException>(() =>
        On(RowContaining("Section")).Of(TextCell().Optional()).Map(space));

      Assert.Contains("no row containing 'Section'", failure.Message);
    }

    // --- Faults are not tolerance ------------------------------------------------------------------------------------
    //
    // A projection that disagreed with the data is what tolerance is for. A projection that simply
    // broke means the reading code is wrong, not the file, and no boundary may quietly swallow it —
    // otherwise a null-reference bug in a map function reads as "this section was absent".

    [Fact]
    public void ANullReferenceInAProjection_IsNotAbsorbed()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Point().Select<ISheetCells, Point<ISheetCells>, string>(_ => throw new NullReferenceException("boom")).Named("bad").Optional().Map(Numbers(1)));

      Assert.Equal("'bad'", failure.Subject);
      Assert.IsType<NullReferenceException>(failure.GetBaseException());
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void AnIndexOutOfRangeInAProjection_IsNotAbsorbed()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Point().Select<ISheetCells, Point<ISheetCells>, string>(_ => throw new IndexOutOfRangeException("boom")).Named("bad").Optional().Map(Numbers(1)));

      Assert.IsType<IndexOutOfRangeException>(failure.GetBaseException());
    }

    [Fact]
    public void ABadViewIndexInAProjection_IsNotAbsorbed()
    {
      // Range(b => b[9, 0]) on a 1x1 extent is a wrong index into the view — the reading code is
      // wrong, not the file — so the view's ArgumentOutOfRangeException must propagate, not read
      // as "this section was absent".
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(b => b[9, 0].Integer()).Named("bad").Optional().Map(Numbers(1)));

      Assert.IsType<ArgumentOutOfRangeException>(failure.GetBaseException());
      Assert.Equal("'bad'", failure.Subject);
    }

    [Fact]
    public void AFaultIsNotAbsorbedByAFallbackProjectionEither()
    {
      // Else would otherwise hide the bug behind a perfectly good fallback reading.
      var failure = Assert.Throws<ProjectionException>(() =>
        Point().Select<ISheetCells, Point<ISheetCells>, string>(_ => throw new NullReferenceException("boom"))
          .Named("bad")
          .Else(Point().Select(_ => "the fallback would have worked"))
          .Map(Numbers(1)));

      Assert.IsType<NullReferenceException>(failure.GetBaseException());
    }

    [Fact]
    public void ADisagreementWithTheDataIsStillAbsorbed()
    {
      // The control: a cell of the wrong kind is the file being unexpected, which is the whole
      // point of a boundary.
      Assert.Null(Title().Optional().Map(Numbers(1)));
    }

    // --- When the fallback fails too ------------------------------------------------------------------------------------

    private static IProjection<ISheetCells, string> PrimaryAndFallbackBothWrong()
      => TextCell().Named("primary")
        .Else(Point().Select(p => p.Date().ToString()).Named("fallback"));

    [Fact]
    public void WhenAFallbackFailsToo_TheFallbackOwnsTheFailure()
    {
      // The fallback is what was being read when the parse finally gave up, so it is what the
      // location and path describe.
      var failure = Assert.Throws<ProjectionException>(() => PrimaryAndFallbackBothWrong().Map(Numbers(1)));

      Assert.Equal("'fallback'", failure.Subject);
      Assert.Equal("'fallback' (Select)", failure.Path);
    }

    [Fact]
    public void WhenAFallbackFailsToo_ThePrimarysFailureIsCarriedAlong()
    {
      // Losing the primary would hide the more interesting half: the reader wants to know why the
      // projection they actually declared did not work, not only that the stand-in failed as well.
      var failure = Assert.Throws<ProjectionException>(() => PrimaryAndFallbackBothWrong().Map(Numbers(1)));

      Assert.Contains("it stands in for 'primary', which failed too: ", failure.Message);
      Assert.Contains("expected Text at A1, found Number", failure.Message);
      Assert.Contains("expected Temporal at A1, found Number", failure.Message);
    }

    [Fact]
    public void WhenAFallbackFailsToo_TheUnannotatedFallbackFailureIsInside()
    {
      var failure = Assert.Throws<ProjectionException>(() => PrimaryAndFallbackBothWrong().Map(Numbers(1)));

      var original = Assert.IsType<ProjectionException>(failure.InnerException);
      Assert.Equal("'fallback'", original.Subject);
      Assert.DoesNotContain("stands in for", original.Message);

      // The base cause is the backend's own read failure. Until phase 6 a wrong-kind read surfaced
      // as an InvalidOperationException, which is the exception a bug throws too.
      Assert.IsType<CellReadException>(failure.GetBaseException());
    }

    // --- The same-origin trap -------------------------------------------------------------------------------------------
    //
    // An absorbed projection consumes nothing, so the sibling after it reads the very cells that just
    // failed — and fails the same way, for the same reason, while blaming itself. The note is the
    // framework saying "the projection before me read nothing, which is probably why I am here".

    private static ISheetCells TextOverNumber() => Mixed(new object?[,] { { "x" }, { 5 } });

    private static IProjection<ISheetCells, string> AbsorbedThenSameCell()
      => VerticalFlow(v => $"{v.Next(IntCell().Optional())}|{v.Next(IntCell())}");

    [Fact]
    public void AFailureRightAfterAnAbsorbedSibling_CarriesANote()
    {
      var failure = Assert.Throws<ProjectionException>(() => AbsorbedThenSameCell().Map(TextOverNumber()));

      Assert.EndsWith(
        "expected Number at A1, found Text; note: the preceding sibling consumed nothing at this position",
        FirstLine(failure));
    }

    [Fact]
    public void TheNoteReplacesOnlyTheFinalStopOfTheProblemItAnnotates()
    {
      // The quoted exception message brings its own full stop; keeping it would read ".; note:".
      var noted = FirstLine(Assert.Throws<ProjectionException>(() => AbsorbedThenSameCell().Map(TextOverNumber())));
      var plain = FirstLine(Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(TextCell())}|{v.Next(TextCell())}").Map(TextOverNumber())));

      // The reading sentence carries no full stop of its own, so there is none to replace — the
      // note simply follows it. The rule is still worth pinning: what must never appear is the
      // doubled punctuation a quoted exception message used to bring with it.
      Assert.EndsWith("found Number", plain);
      Assert.DoesNotContain("Text.;", noted);
      Assert.DoesNotContain(".;", noted);
      Assert.DoesNotContain("note:", plain);
    }

    [Fact]
    public void TheNoteDoesNotChangeWhoOwnsTheFailure()
    {
      // The sibling still owns the failure; the note only points at what probably caused it.
      var failure = Assert.Throws<ProjectionException>(() => AbsorbedThenSameCell().Map(TextOverNumber()));

      // The inferred use-site label names the subject as well as the path, so an inline child is
      // "Integer#2" in both halves of the message rather than disagreeing with itself.
      Assert.Equal("Integer#2", failure.Subject);
      Assert.Equal("VerticalFlow -> Integer#2", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void ANotedFailureKeepsTheUnannotatedOriginalInside()
    {
      var failure = Assert.Throws<ProjectionException>(() => AbsorbedThenSameCell().Map(TextOverNumber()));

      var original = Assert.IsType<ProjectionException>(failure.InnerException);
      Assert.DoesNotContain("note:", original.Message);
      Assert.Equal(failure.Subject, original.Subject);
      Assert.Equal(failure.Path, original.Path);

      // ...and the root cause is still one hop away from anyone who wants it. It is the unannotated
      // failure itself now: a kinded leaf reports its own read, so there is nothing thrown beneath
      // it to unwrap. Until phase 6 the base was the InvalidOperationException the leaf threw,
      // whose message was "Cell value is Text; expected Number.".
      var cause = Assert.IsType<ProjectionException>(failure.GetBaseException());
      Assert.Same(original, cause);
      Assert.Equal("expected Number at A1, found Text", cause.Problem);
    }

    [Fact]
    public void AFlowWhoseSiblingsAllConsume_GainsNoNote()
    {
      // Nothing consumed nothing, so there is nothing to blame but the projection that failed.
      var laterChild = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(TextCell())}|{v.Next(TextCell())}").Map(TextOverNumber()));

      var firstChild = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}").Map(TextOverNumber()));

      Assert.DoesNotContain("note:", laterChild.Message);
      Assert.DoesNotContain("note:", firstChild.Message);
    }

    [Fact]
    public void OnlyTheImmediatelyFollowingSiblingIsNoted()
    {
      // The second child reads the absorbed projection's cells successfully and moves the cursor
      // on, so by the time the third child fails the coincidence has passed.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
          $"{v.Next(IntCell().Optional())}|{v.Next(TextCell())}|{v.Next(TextCell())}")
          .Map(TextOverNumber()));

      Assert.DoesNotContain("note:", failure.Message);
    }

    [Fact]
    public void ASiblingThatReAnchoredItselfAndFailedElsewhere_IsNotNoted()
    {
      // The note is about a coincidence of position. This child skipped past the vacated cell and
      // failed three rows down on its own account, so blaming the absorbed sibling would be a
      // guess.
      var space = Mixed(new object?[,] { { "x" }, { null }, { null }, { 5 }, { 6 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
          $"{v.Next(IntCell().Optional())}|{v.Next(OffsetBy(BlankRows()).Down(2).Of(TextCell()))}")
          .Map(space));

      Assert.DoesNotContain("note:", failure.Message);
      Assert.Equal("A3", failure.Location.A1);
    }

    [Fact]
    public void TheNoteIsAboutConsumptionRatherThanAboutAbsorption()
    {
      // Any sibling that consumed nothing leaves the next one in the same position; a boundary is
      // simply the usual way that happens.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
          $"{v.Next(Range(AreaStrategies.ExplicitArea(1, 0), b => b.Height))}|{v.Next(IntCell())}")
          .Map(TextOverNumber()));

      Assert.Contains("note: the preceding sibling consumed nothing at this position", failure.Message);
    }

    [Fact]
    public void AHorizontalFlowIsNotedTheSameWay()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        HorizontalFlow(h => $"{h.Next(IntCell().Optional())}|{h.Next(IntCell())}")
          .Map(Mixed(new object?[,] { { "x", 5 } })));

      Assert.Contains("note: the preceding sibling consumed nothing at this position", failure.Message);
    }

    [Fact]
    public void TheNoteSurvivesTheRollbackOfALosingChoiceBranch()
    {
      // The payoff. Inside a choice, a losing branch's absorption Warning is rolled back with the
      // branch — so the note carried by the failure itself is the only surviving evidence that the
      // branch tolerated something before it died.
      var tolerant = VerticalFlow(v =>
      {
        v.Next(IntCell().Optional());
        return v.Next(IntCell());
      }).Named("tolerant branch");

      var strict = VerticalFlow(v =>
      {
        v.Next(IntCell());
        return v.Next(IntCell());
      }).Named("strict branch");

      var failure = Assert.Throws<ProjectionException>(() => Choice(tolerant, strict).Map(TextOverNumber()));

      Assert.Contains(
        "alternative 1 ('tolerant branch'): "
        + "expected Number at A1, found Text; note: the preceding sibling consumed nothing at this position",
        failure.Message);

      // The branch that tolerated nothing says so by having nothing to say.
      Assert.Contains("alternative 2 ('strict branch'): ", failure.Message);
      Assert.Equal(1, Occurrences(failure.Message, "note:"));
    }

    private static string FirstLine(ProjectionException failure)
      => failure.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];


    // --- Transparency and inspection ----------------------------------------------------------------------------------

    [Fact]
    public void AnUnnamedBoundaryContributesNoPathSegment()
    {
      var warning = Warning(Title().Optional().MapWithDiagnostics(Numbers(1)));

      Assert.DoesNotContain("Optional", warning.Path);
    }

    [Fact]
    public void ANamedBoundaryContributesAPathSegment()
    {
      var warning = Warning(Title().Optional().Named("the header").MapWithDiagnostics(Numbers(1)));

      Assert.Equal("'the header' -> 'title' (Text)", warning.Path);
    }

    [Fact]
    public void ABoundaryDescribesItselfAndExposesWhatItWraps()
    {
      var inner = IntCell().Named("inner");
      var fallback = IntCell().Named("fallback");

      Assert.Equal("Optional", inner.Optional().Description);
      Assert.Equal("Else", inner.Else(0).Description);
      Assert.Equal("Else", inner.Else(fallback).Description);

      Assert.Single(inner.Else(0).Children);
      Assert.Equal(2, inner.Else(fallback).Children.Count);
      Assert.Same(inner, inner.Else(fallback).Children[0]);
      Assert.Same(fallback, inner.Else(fallback).Children[1]);
    }

    [Fact]
    public void ABoundaryIsAWrapperWhetherOrNotItIsNamed()
    {
      Assert.True(Title().Optional().IsWrapper);
      Assert.True(Title().Optional().Named("named").IsWrapper);
    }

    // --- Argument guards --------------------------------------------------------------------------------------------------

    [Fact]
    public void Else_RejectsANullFallbackProjection()
    {
      Assert.Equal("fallback", Assert.Throws<ArgumentNullException>(() => Title().Else((IProjection<ISheetCells, string>)null!)).ParamName);
    }

    [Fact]
    public void BoundariesRejectANullProjection()
    {
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<ISheetCells, int>)null!).Optional()).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<ISheetCells, int>)null!).Else(0)).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<ISheetCells, int>)null!).Else(IntCell())).ParamName);
    }
  }
}
