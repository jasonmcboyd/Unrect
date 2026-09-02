using System;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// Ordered alternatives against one extent: the first that matches wins, and every alternative
  /// passed over leaves an <c>Info</c> saying why it did not. An alternative that fails on its way
  /// out takes whatever it tolerated with it, so a branch that did not win says exactly one thing.
  /// </summary>
  public class ChoiceShapeTests
  {
    // One column, two rows: a label over a number. The winning alternative below consumes both, so
    // these tests see no unconsumed-space diagnostic to filter out.
    private static ISpace Pair() => Mixed(new object?[,] { { "x" }, { 5 } });

    /// <summary>Reads the pair as text-then-number: what the file actually is.</summary>
    private static IShape<int> TextFirst(string name = "vendor A layout")
      => VerticalFlow(v => { v.Next(Cell(c => c.GetString())); return v.Next(Cell(c => c.GetInt())); }).Named(name);

    /// <summary>Reads the pair as number-then-number: a layout this file is not in.</summary>
    private static IShape<int> NumberFirst(string name = "vendor B layout")
      => VerticalFlow(v => { v.Next(Cell(c => c.GetInt())); return v.Next(Cell(c => c.GetInt())); }).Named(name);

    // --- Choosing ------------------------------------------------------------------------------------

    [Fact]
    public void Choice_TakesTheFirstAlternativeThatMatches()
    {
      Assert.Equal(5, Choice(TextFirst(), NumberFirst()).Map(Pair()));
    }

    [Fact]
    public void Choice_WhenTheFirstAlternativeMatches_ReportsNothing()
    {
      // Alternation that goes right the first time is not news.
      var result = Choice(TextFirst(), NumberFirst()).MapWithDiagnostics(Pair());

      Assert.Equal(5, result.Value);
      Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Choice_TriesAlternativesInDeclarationOrder()
    {
      Assert.Equal(5, Choice(NumberFirst(), TextFirst()).Map(Pair()));
    }

    [Fact]
    public void Choice_ReportsAnInfoForEachAlternativeItPassedOver()
    {
      var result = Choice(NumberFirst(), TextFirst()).MapWithDiagnostics(Pair());

      var info = Assert.Single(result.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, info.Severity);
      Assert.Equal("Choice", info.Subject);
      Assert.StartsWith("alternative 1 ('vendor B layout') did not match: ", info.Message);
      Assert.Contains("Cell value is Text; expected Number", info.Message);
    }

    [Fact]
    public void APassedOverAlternativesInfo_CarriesTheInnerFailuresPathAndLocation()
    {
      // The useful fact is which shape inside the alternative gave up and where, not that a Choice
      // was involved — the Info points at the cause the same way an exception would.
      var result = Choice(NumberFirst(), TextFirst()).MapWithDiagnostics(Pair());

      var info = Assert.Single(result.Diagnostics);

      Assert.Equal("Choice -> 'vendor B layout' -> Cell#1", info.Path);
      Assert.Equal("A1", info.Location.A1);
    }

    [Fact]
    public void ATransparentAlternative_IsDescribedByWhatItWraps()
    {
      // Variants are unified with an unnamed Select, and "alternative 1 (Select) did not match"
      // says nothing. A transparent wrapper is described by what is inside it, exactly as it is
      // skipped in a path.
      var result = Choice(NumberFirst(), TextFirst()).MapWithDiagnostics(Pair());

      var info = Assert.Single(result.Diagnostics);

      Assert.Contains("('vendor B layout')", info.Message);
      Assert.DoesNotContain("Select", info.Message);
      Assert.DoesNotContain("Select", info.Path);
    }

    [Fact]
    public void AnUnnamedAlternative_IsDescribedStructurally()
    {
      var alternatives = Choice(
        VerticalFlow(v => { v.Next(Cell(c => c.GetInt())); return v.Next(Cell(c => c.GetInt())); }),
        TextFirst());

      var info = Assert.Single(alternatives.MapWithDiagnostics(Pair()).Diagnostics);

      Assert.StartsWith("alternative 1 (VerticalFlow) did not match: ", info.Message);
    }

    [Fact]
    public void Choice_ConsumesWhatTheWinningAlternativeConsumed()
    {
      var applied = Choice(NumberFirst(), TextFirst()).Apply(Pair());

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    // --- When nothing matches -------------------------------------------------------------------------

    [Fact]
    public void Choice_WhenNoAlternativeMatches_ThrowsAtItsOwnPath()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Choice(NumberFirst("first try"), NumberFirst("second try")).Map(Pair()));

      Assert.Equal("Choice", failure.Subject);
      Assert.Equal("Choice", failure.Path);
    }

    [Fact]
    public void Choice_WhenNoAlternativeMatches_ListsEveryNearMiss()
    {
      // The failure reads like a diff of the layouts that were tried, so the reader can see which
      // one nearly worked rather than only that none did.
      var failure = Assert.Throws<ShapeException>(() =>
        Choice(NumberFirst("first try"), NumberFirst("second try")).Map(Pair()));

      Assert.Contains("no alternative matched", failure.Message);
      Assert.Contains("alternative 1 ('first try'): ", failure.Message);
      Assert.Contains("alternative 2 ('second try'): ", failure.Message);
      Assert.Contains("Cell value is Text; expected Number", failure.Message);
      Assert.Contains("at row 1, column 1 (A1)", failure.Message);
    }

    [Fact]
    public void Choice_WhenNoAlternativeMatches_KeepsTheLastFailureAsTheInnerException()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Choice(NumberFirst("first try"), NumberFirst("second try")).Map(Pair()));

      var inner = Assert.IsType<ShapeException>(failure.InnerException);
      Assert.Equal("Choice -> 'second try' -> Cell#1", inner.Path);
    }

    [Fact]
    public void AFaultInAnAlternative_DoesNotFallThroughToTheNext()
    {
      // A broken map function is a bug in the reading code, not a layout that did not match. Trying
      // the next alternative would turn it into a silently different parse.
      var failure = Assert.Throws<ShapeException>(() =>
        Choice(
          Cell<int>(_ => throw new NullReferenceException("boom")).Named("first"),
          Cell(v => v.GetInt()).Named("second"))
          .Map(Mixed(new object?[,] { { 5 } })));

      Assert.Equal("'first'", failure.Subject);
      Assert.IsType<NullReferenceException>(failure.GetBaseException());
      Assert.DoesNotContain("no alternative matched", failure.Message);
    }

    // --- The aggregate as a diagnostic --------------------------------------------------------------------

    [Fact]
    public void AnAggregateFailureFoldsOntoOneLineWhenItIsReportedRatherThanThrown()
    {
      // Thrown, the tally is a block a reader scans down. Recorded, it is one entry in a list of
      // diagnostics, so the alternatives become clauses instead of lines.
      var space = Mixed(new object?[,] { { "text" } });

      var choice = Choice(
        Cell(v => v.GetInt().ToString()).Named("a"),
        Cell(v => v.GetDateTime().ToString()).Named("b"));

      var reported = Assert.Single(choice.Optional().MapWithDiagnostics(space).Diagnostics);

      Assert.Equal("Choice", reported.Subject);
      Assert.StartsWith("no alternative matched; alternative 1 ('a'): ", reported.Message);
      Assert.Contains("; alternative 2 ('b'): ", reported.Message);
      Assert.DoesNotContain('\n', reported.Message);
      Assert.DoesNotContain('\n', reported.ToString());
    }

    // --- Rollback ---------------------------------------------------------------------------------------

    [Fact]
    public void ALosingAlternativesOwnDiagnostics_DoNotSurvive()
    {
      // The first alternative tolerates something and then fails anyway. Its warning describes a
      // reading that was thrown away, so keeping it would describe a parse that never happened.
      var losing = VerticalFlow(v =>
      {
        v.Next(Cell(c => c.GetString()).Optional());
        v.Next(Cell(c => c.GetString()));
        return 0;
      }).Named("losing");

      var result = Choice(losing, TextFirst()).MapWithDiagnostics(Pair());

      Assert.Equal(5, result.Value);
      Assert.All(result.Diagnostics, d => Assert.Equal(DiagnosticSeverity.Info, d.Severity));
      Assert.Single(result.Diagnostics);
    }

    [Fact]
    public void TheWinningAlternativesOwnDiagnostics_Survive()
    {
      // Tolerance exercised by the branch that actually produced the result is part of the result.
      var winning = VerticalFlow(v =>
      {
        v.Next(Cell(c => c.GetString()));
        v.Next(Cell(c => c.GetString()).Optional());
        return v.Next(Cell(c => c.GetInt()));
      }).Named("winning");

      var result = Choice(NumberFirst(), winning).MapWithDiagnostics(Pair());

      Assert.Equal(5, result.Value);
      Assert.Equal(2, result.Diagnostics.Count);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Equal("Choice -> 'winning' -> Cell#2", warning.Path);
    }

    // --- Inspection ----------------------------------------------------------------------------------------

    [Fact]
    public void AChoiceDescribesItselfAndExposesItsAlternatives()
    {
      var first = TextFirst();
      var second = NumberFirst();

      var choice = Choice(first, second);

      Assert.Equal("Choice", choice.Description);
      Assert.Equal(2, choice.Children.Count);
      Assert.Same(first, choice.Children[0]);
      Assert.Same(second, choice.Children[1]);
      Assert.False(choice.IsTransparent);
      Assert.Null(choice.Placement.Area);
    }

    [Fact]
    public void AChoiceIsAShapeAndCanBeNamedAndPlaced()
    {
      var space = Mixed(new object?[,] { { null }, { "x" }, { 5 } });

      var shape = Choice(NumberFirst(), TextFirst()).AfterBlankRows().Named("layout");

      Assert.Equal(5, shape.Map(space));
      Assert.Equal("layout", shape.Name);
    }

    [Fact]
    public void AChoiceCopiesItsAlternatives()
    {
      // The array a caller passed stays theirs to mutate; the shape is a value.
      var alternatives = new[] { TextFirst(), NumberFirst() };
      var choice = Choice(alternatives);

      alternatives[0] = NumberFirst();

      Assert.Equal(5, choice.Map(Pair()));
    }

    // --- Factory validation -----------------------------------------------------------------------------------

    [Fact]
    public void Choice_RejectsANullArray()
    {
      Assert.Equal("alternatives", Assert.Throws<ArgumentNullException>(() => Choice<int>(null!)).ParamName);
    }

    [Fact]
    public void Choice_RejectsANullAlternative()
    {
      var failure = Assert.Throws<ArgumentException>(() => Choice<int>(TextFirst(), null!));

      Assert.Equal("alternatives", failure.ParamName);
      Assert.Contains("Alternative 2 is null", failure.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Choice_NeedsAtLeastTwoAlternatives(int count)
    {
      // One alternative is not a choice; zero is not a shape.
      var alternatives = Enumerable.Range(0, count).Select(_ => TextFirst()).ToArray();

      var failure = Assert.Throws<ArgumentException>(() => Choice(alternatives));

      Assert.Equal("alternatives", failure.ParamName);
      Assert.Contains("at least two alternatives", failure.Message);
    }
  }
}
