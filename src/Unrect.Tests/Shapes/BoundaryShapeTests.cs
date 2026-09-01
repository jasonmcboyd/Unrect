using System;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// <c>Else</c> and <c>Optional</c> declare tolerance at the one shape where it is acceptable.
  /// They behave like a catch block: everything underneath still fails exactly as loudly, the
  /// failure travels up to the nearest boundary, and the boundary records what actually went wrong
  /// before supplying a filler. There is no lenient mode to switch on.
  /// </summary>
  public class BoundaryShapeTests
  {
    // One column of numbers, so a shape asking for text is a guaranteed, well-located failure.
    private static ISpace Numbers(int height = 3)
    {
      var values = new int[height, 1];

      for (var row = 0; row < height; row++)
        values[row, 0] = row + 1;

      return Grid(values);
    }

    private static IShape<string> Title() => Cell(v => v.GetString()).Named("title");

    /// <summary>
    /// The one warning a parse produced. An absorbing boundary consumes nothing, so a boundary at
    /// the root also leaves an unconsumed-space Info covering the whole sheet; the warning is the
    /// part these tests are about.
    /// </summary>
    private static ShapeDiagnostic Warning<T>(MapResult<T> result)
      => Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

    // --- What a boundary yields -----------------------------------------------------------------------

    [Fact]
    public void Optional_YieldsTheDefaultWhenTheShapeFails()
    {
      Assert.Null(Title().Optional().Map(Numbers()));
    }

    [Fact]
    public void Optional_OnAValueType_YieldsThatTypesDefault()
    {
      // Not null — the filler for an int is 0. Where "absent" and "zero" must differ, Else(value)
      // or a projection to a nullable says so explicitly.
      Assert.Equal(0, Cell(v => v.GetString()).Select(text => text.Length).Optional().Map(Numbers()));
    }

    [Fact]
    public void ElseValue_YieldsTheConstantWhenTheShapeFails()
    {
      Assert.Equal("missing", Title().Else("missing").Map(Numbers()));
    }

    [Fact]
    public void ElseShape_YieldsTheFallbacksReadingWhenTheShapeFails()
    {
      Assert.Equal("1", Title().Else(Cell(v => v.GetInt().ToString()).Named("plan B")).Map(Numbers()));
    }

    [Fact]
    public void ABoundaryIsInertWhenTheShapeSucceeds()
    {
      var space = Mixed(new object?[,] { { "Acme" } });

      var result = Cell(v => v.GetString()).Named("title").Optional().MapWithDiagnostics(space);

      Assert.Equal("Acme", result.Value);
      Assert.Empty(result.Diagnostics);
    }

    // --- What a boundary reports ------------------------------------------------------------------------

    [Fact]
    public void AnAbsorbedFailure_IsReportedAsAWarning()
    {
      // Info is for things going as designed; tolerance being exercised means the file was not
      // what the shape says it should be.
      var diagnostics = Title().Optional().MapWithDiagnostics(Numbers(1)).Diagnostics;

      Assert.Single(diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void AnAbsorbedFailure_IsDescribedByTheShapeThatFailedNotTheBoundary()
    {
      // The boundary caught it; the Cell caused it. A warning is only actionable if it names the
      // latter.
      var warning = Warning(Title().Optional().MapWithDiagnostics(Numbers(1)));

      Assert.Equal("'title'", warning.Subject);
      Assert.Equal("'title' (Cell)", warning.Path);
      Assert.Contains("Cell value is Number; expected Text", warning.Message);
      Assert.Equal("A1", warning.Location.A1);
    }

    [Fact]
    public void EveryBoundarySpellingReportsTheSameFailure()
    {
      var optional = Warning(Title().Optional().MapWithDiagnostics(Numbers(1)));
      var elseValue = Warning(Title().Else("x").MapWithDiagnostics(Numbers(1)));
      var elseShape = Warning(Title().Else(Cell(v => "y").Named("plan B")).MapWithDiagnostics(Numbers(1)));

      Assert.Equal(optional.Message, elseValue.Message);
      Assert.Equal(optional.Message, elseShape.Message);
      Assert.Equal("'title' (Cell)", elseShape.Path);
      Assert.All(
        new[] { optional, elseValue, elseShape },
        d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
    }

    // --- What a boundary consumes -------------------------------------------------------------------------

    [Fact]
    public void AnAbsorbingBoundaryConsumesNothing()
    {
      // Nothing was read, so no honest extent exists. A following sibling starts where the failed
      // shape began rather than after it — which is why absorbing boundaries want seek-anchored
      // siblings rather than arithmetic.
      var applied = Title().Optional().Apply(Numbers());

      Assert.Equal(0, applied.Consumed.Width);
      Assert.Equal(0, applied.Consumed.Height);
    }

    [Fact]
    public void AFollowingSiblingStartsWhereTheAbsorbedShapeBegan()
    {
      var (title, first, second) = Vertical(
        Title().Optional(),
        Cell(v => v.GetInt()),
        Cell(v => v.GetInt()))
        .Map(Numbers());

      Assert.Null(title);
      Assert.Equal(1, first);
      Assert.Equal(2, second);
    }

    [Fact]
    public void ElseValue_AlsoConsumesNothing()
    {
      var (title, first) = Vertical(Title().Else("missing"), Cell(v => v.GetInt())).Map(Numbers());

      Assert.Equal("missing", title);
      Assert.Equal(1, first);
    }

    [Fact]
    public void ElseShape_ConsumesWhateverTheFallbackConsumed()
    {
      // A fallback shape did read something, so it reports an honest extent and the next sibling
      // clears it.
      var (title, next) = Vertical(
        Title().Else(Cell(v => v.GetInt().ToString()).Named("plan B")),
        Cell(v => v.GetInt()))
        .Map(Numbers());

      Assert.Equal("1", title);
      Assert.Equal(2, next);
    }

    [Fact]
    public void ElseShape_ReportsTheFallbacksAdvance()
    {
      var applied = Cells(b => b.Width).Select(w => "wide").Else(Cells(1, 2, b => "narrow")).Apply(Numbers());

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    // --- Depth ------------------------------------------------------------------------------------------------

    [Fact]
    public void ABoundaryAbsorbsAFailureFromDeepInsideIt()
    {
      // Three levels down: the boundary wraps a stack whose second child fails. Nothing between
      // them softens anything — the failure travels to the nearest boundary and stops there.
      var shape = Vertical(
        Cell(v => v.GetInt()),
        Vertical(Cell(v => v.GetInt()), Cell(v => v.GetString())).Select((first, second) => second).Optional(),
        Cell(v => v.GetInt()));

      var result = shape.MapWithDiagnostics(Numbers());

      var (before, absorbed, after) = result.Value;
      Assert.Equal(1, before);
      Assert.Null(absorbed);
      Assert.Equal(2, after);
    }

    [Fact]
    public void ADeeplyAbsorbedFailure_KeepsItsFullPathAndTrueLocation()
    {
      var shape = Vertical(
        Cell(v => v.GetInt()),
        Vertical(Cell(v => v.GetInt()), Cell(v => v.GetString())).Select((first, second) => second).Optional(),
        Cell(v => v.GetInt()));

      var warning = Assert.Single(
        shape.MapWithDiagnostics(Numbers()).Diagnostics,
        d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("Vertical -> Vertical -> Cell", warning.Path);
      Assert.Equal("A3", warning.Location.A1);
    }

    // --- Where the boundary sits decides what it can catch ---------------------------------------------------------

    [Fact]
    public void ABoundaryInsideTheAnchor_AbsorbsAMissingAnchor()
    {
      // The boundary's own placement resolves before it can catch anything, so an offset written
      // inside it is inside the try block.
      var space = Mixed(new object?[,] { { "nothing" }, { "here" } });

      var result = Cell(v => v.GetString()).After(SeekRowContaining("Section")).Optional().MapWithDiagnostics(space);

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

      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetString()).Optional().After(SeekRowContaining("Section")).Map(space));

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
      var failure = Assert.Throws<ShapeException>(() =>
        Cell<string>(_ => throw new NullReferenceException("boom")).Named("bad").Optional().Map(Numbers(1)));

      Assert.Equal("'bad'", failure.Subject);
      Assert.IsType<NullReferenceException>(failure.GetBaseException());
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void AnIndexOutOfRangeInAProjection_IsNotAbsorbed()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Cell<string>(_ => throw new IndexOutOfRangeException("boom")).Named("bad").Optional().Map(Numbers(1)));

      Assert.IsType<IndexOutOfRangeException>(failure.GetBaseException());
    }

    [Fact]
    public void ABadViewIndexInAProjection_IsNotAbsorbed()
    {
      // Cells(b => b[9, 0]) on a 1x1 extent is a wrong index into the view — the reading code is
      // wrong, not the file — so the view's ArgumentOutOfRangeException must propagate, not read
      // as "this section was absent".
      var failure = Assert.Throws<ShapeException>(() =>
        Cells(b => b[9, 0].GetInt()).Named("bad").Optional().Map(Numbers(1)));

      Assert.IsType<ArgumentOutOfRangeException>(failure.GetBaseException());
      Assert.Equal("'bad'", failure.Subject);
    }

    [Fact]
    public void AFaultIsNotAbsorbedByAFallbackShapeEither()
    {
      // Else would otherwise hide the bug behind a perfectly good fallback reading.
      var failure = Assert.Throws<ShapeException>(() =>
        Cell<string>(_ => throw new NullReferenceException("boom"))
          .Named("bad")
          .Else(Cell(v => "the fallback would have worked"))
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

    private static IShape<string> PrimaryAndFallbackBothWrong()
      => Cell(v => v.GetString()).Named("primary")
        .Else(Cell(v => v.GetDateTime().ToString()).Named("fallback"));

    [Fact]
    public void WhenAFallbackFailsToo_TheFallbackOwnsTheFailure()
    {
      // The fallback is what was being read when the parse finally gave up, so it is what the
      // location and path describe.
      var failure = Assert.Throws<ShapeException>(() => PrimaryAndFallbackBothWrong().Map(Numbers(1)));

      Assert.Equal("'fallback'", failure.Subject);
      Assert.Equal("'fallback' (Cell)", failure.Path);
    }

    [Fact]
    public void WhenAFallbackFailsToo_ThePrimarysFailureIsCarriedAlong()
    {
      // Losing the primary would hide the more interesting half: the reader wants to know why the
      // shape they actually declared did not work, not only that the stand-in failed as well.
      var failure = Assert.Throws<ShapeException>(() => PrimaryAndFallbackBothWrong().Map(Numbers(1)));

      Assert.Contains("it stands in for 'primary', which failed too: ", failure.Message);
      Assert.Contains("Cell value is Number; expected Text", failure.Message);
      Assert.Contains("Cell value is Number; expected Temporal", failure.Message);
    }

    [Fact]
    public void WhenAFallbackFailsToo_TheUnannotatedFallbackFailureIsInside()
    {
      var failure = Assert.Throws<ShapeException>(() => PrimaryAndFallbackBothWrong().Map(Numbers(1)));

      var original = Assert.IsType<ShapeException>(failure.InnerException);
      Assert.Equal("'fallback'", original.Subject);
      Assert.DoesNotContain("stands in for", original.Message);
      Assert.IsType<InvalidOperationException>(failure.GetBaseException());
    }

    // --- The same-origin trap -------------------------------------------------------------------------------------------
    //
    // An absorbed shape consumes nothing, so the sibling after it reads the very cells that just
    // failed — and fails the same way, for the same reason, while blaming itself. The note is the
    // framework saying "the shape before me read nothing, which is probably why I am here".

    private static ISpace TextOverNumber() => Mixed(new object?[,] { { "x" }, { 5 } });

    private static IShape<(int, int)> AbsorbedThenSameCell()
      => Vertical(Cell(v => v.GetInt()).Optional(), Cell(v => v.GetInt()));

    [Fact]
    public void AFailureRightAfterAnAbsorbedSibling_CarriesANote()
    {
      var failure = Assert.Throws<ShapeException>(() => AbsorbedThenSameCell().Map(TextOverNumber()));

      Assert.EndsWith(
        "Cell value is Text; expected Number; note: the preceding sibling consumed nothing at this position",
        FirstLine(failure));
    }

    [Fact]
    public void TheNoteReplacesOnlyTheFinalStopOfTheProblemItAnnotates()
    {
      // The quoted exception message brings its own full stop; keeping it would read ".; note:".
      var noted = FirstLine(Assert.Throws<ShapeException>(() => AbsorbedThenSameCell().Map(TextOverNumber())));
      var plain = FirstLine(Assert.Throws<ShapeException>(() =>
        Vertical(Cell(v => v.GetString()), Cell(v => v.GetString())).Map(TextOverNumber())));

      Assert.EndsWith("expected Text.", plain);
      Assert.DoesNotContain("Number.;", noted);
      Assert.DoesNotContain("note:", plain);
    }

    [Fact]
    public void TheNoteDoesNotChangeWhoOwnsTheFailure()
    {
      // The sibling still owns the failure; the note only points at what probably caused it.
      var failure = Assert.Throws<ShapeException>(() => AbsorbedThenSameCell().Map(TextOverNumber()));

      Assert.Equal("Cell", failure.Subject);
      Assert.Equal("Vertical -> Cell", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void ANotedFailureKeepsTheUnannotatedOriginalInside()
    {
      var failure = Assert.Throws<ShapeException>(() => AbsorbedThenSameCell().Map(TextOverNumber()));

      var original = Assert.IsType<ShapeException>(failure.InnerException);
      Assert.DoesNotContain("note:", original.Message);
      Assert.Equal(failure.Subject, original.Subject);
      Assert.Equal(failure.Path, original.Path);

      // ...and the root cause is still one hop away from anyone who wants it.
      var cause = Assert.IsType<InvalidOperationException>(failure.GetBaseException());
      Assert.Equal("Cell value is Text; expected Number.", cause.Message);
    }

    [Fact]
    public void AFlowWhoseSiblingsAllConsume_GainsNoNote()
    {
      // Nothing consumed nothing, so there is nothing to blame but the shape that failed.
      var laterChild = Assert.Throws<ShapeException>(() =>
        Vertical(Cell(v => v.GetString()), Cell(v => v.GetString())).Map(TextOverNumber()));

      var firstChild = Assert.Throws<ShapeException>(() =>
        Vertical(Cell(v => v.GetInt()), Cell(v => v.GetInt())).Map(TextOverNumber()));

      Assert.DoesNotContain("note:", laterChild.Message);
      Assert.DoesNotContain("note:", firstChild.Message);
    }

    [Fact]
    public void OnlyTheImmediatelyFollowingSiblingIsNoted()
    {
      // The second child reads the absorbed shape's cells successfully and moves the cursor on, so
      // by the time the third child fails the coincidence has passed.
      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(
          Cell(v => v.GetInt()).Optional(),
          Cell(v => v.GetString()),
          Cell(v => v.GetString()))
          .Map(TextOverNumber()));

      Assert.DoesNotContain("note:", failure.Message);
    }

    [Fact]
    public void ASiblingThatReAnchoredItselfAndFailedElsewhere_IsNotNoted()
    {
      // The note is about a coincidence of position. This child skipped past the vacated cell and
      // failed three rows down on its own account, so blaming the absorbed sibling would be a guess.
      var space = Mixed(new object?[,] { { "x" }, { null }, { null }, { 5 }, { 6 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(
          Cell(v => v.GetInt()).Optional(),
          Cell(v => v.GetString()).After(BlankRows()).Down(2))
          .Map(space));

      Assert.DoesNotContain("note:", failure.Message);
      Assert.Equal("A3", failure.Location.A1);
    }

    [Fact]
    public void TheNoteIsAboutConsumptionRatherThanAboutAbsorption()
    {
      // Any sibling that consumed nothing leaves the next one in the same position; a boundary is
      // simply the usual way that happens.
      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(
          Cells(AreaStrategies.ExplicitArea(1, 0), b => b.Height),
          Cell(v => v.GetInt()))
          .Map(TextOverNumber()));

      Assert.Contains("note: the preceding sibling consumed nothing at this position", failure.Message);
    }

    [Fact]
    public void AHorizontalFlowIsNotedTheSameWay()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Horizontal(Cell(v => v.GetInt()).Optional(), Cell(v => v.GetInt()))
          .Map(Mixed(new object?[,] { { "x", 5 } })));

      Assert.Contains("note: the preceding sibling consumed nothing at this position", failure.Message);
    }

    [Fact]
    public void TheNoteSurvivesTheRollbackOfALosingChoiceBranch()
    {
      // The payoff. Inside a choice, a losing branch's absorption Warning is rolled back with the
      // branch — so the note carried by the failure itself is the only surviving evidence that the
      // branch tolerated something before it died.
      var tolerant = Vertical(Cell(v => v.GetInt()).Optional(), Cell(v => v.GetInt()))
        .Named("tolerant branch")
        .Select((absorbed, value) => value);

      var strict = Vertical(Cell(v => v.GetInt()), Cell(v => v.GetInt()))
        .Named("strict branch")
        .Select((first, second) => second);

      var failure = Assert.Throws<ShapeException>(() => Choice(tolerant, strict).Map(TextOverNumber()));

      Assert.Contains(
        "alternative 1 ('tolerant branch'): the projection threw InvalidOperationException: "
        + "Cell value is Text; expected Number; note: the preceding sibling consumed nothing at this position",
        failure.Message);

      // The branch that tolerated nothing says so by having nothing to say.
      Assert.Contains("alternative 2 ('strict branch'): ", failure.Message);
      Assert.Equal(1, Occurrences(failure.Message, "note:"));
    }

    private static string FirstLine(ShapeException failure)
      => failure.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];

    private static int Occurrences(string text, string value)
    {
      var count = 0;

      for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
        count++;

      return count;
    }

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

      Assert.Equal("'the header' -> 'title' (Cell)", warning.Path);
    }

    [Fact]
    public void ABoundaryDescribesItselfAndExposesWhatItWraps()
    {
      var inner = Cell(v => v.GetInt()).Named("inner");
      var fallback = Cell(v => v.GetInt()).Named("fallback");

      Assert.Equal("Optional", inner.Optional().Description);
      Assert.Equal("Else", inner.Else(0).Description);
      Assert.Equal("Else", inner.Else(fallback).Description);

      Assert.Single(inner.Else(0).Children);
      Assert.Equal(2, inner.Else(fallback).Children.Count);
      Assert.Same(inner, inner.Else(fallback).Children[0]);
      Assert.Same(fallback, inner.Else(fallback).Children[1]);
    }

    [Fact]
    public void OnlyAnUnnamedBoundaryIsTransparent()
    {
      Assert.True(Title().Optional().IsTransparent);
      Assert.False(Title().Optional().Named("named").IsTransparent);
    }

    // --- Argument guards --------------------------------------------------------------------------------------------------

    [Fact]
    public void Else_RejectsANullFallbackShape()
    {
      Assert.Equal("fallback", Assert.Throws<ArgumentNullException>(() => Title().Else((IShape<string>)null!)).ParamName);
    }

    [Fact]
    public void BoundariesRejectANullShape()
    {
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Optional()).ParamName);
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Else(0)).ParamName);
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Else(Cell(v => v.GetInt()))).ParamName);
    }
  }
}
