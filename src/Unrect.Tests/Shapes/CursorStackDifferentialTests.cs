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
  /// The load-bearing suite. Both spellings of a flow — children as arguments, children as
  /// <c>Next</c> calls — run the same arithmetic through the same <c>FlowState</c>, and the claim
  /// that only tooling can tell them apart is what makes the experiment cheap to judge.
  /// <para>
  /// Each test declares the same thing twice and asserts that the value, the offset, the consumed
  /// extent, and the advance all agree — and, where the declaration fails, that the path, the cell,
  /// and the message agree too. If a future edit re-forks the flow arithmetic, this is what notices.
  /// </para>
  /// </summary>
  public class CursorStackDifferentialTests
  {
    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    private static ISpace Ladder() => Grid(new[,] { { 1 }, { 2 }, { 3 } });

    // 4 columns by 3 rows of (row * 10 + column + 1): 1 2 3 4 / 11 12 13 14 / 21 22 23 24.
    private static ISpace CoordinateGrid()
    {
      var values = new int[3, 4];

      for (var row = 0; row < 3; row++)
        for (var column = 0; column < 4; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    /// <summary>
    /// Applies both spellings and asserts they are indistinguishable: the same value in the same
    /// place, the same diagnostics on the way, and — where the declaration fails — the same failure.
    /// </summary>
    private static void AssertIndistinguishable<T>(IShape<T> applicative, IShape<T> cursor, ISpace space)
    {
      AppliedResult<T> byArguments = default;
      AppliedResult<T> byCursor = default;

      var expected = Record.Exception(() => { byArguments = applicative.Apply(space); });
      var actual = Record.Exception(() => { byCursor = cursor.Apply(space); });

      AssertAgreeOnWhetherTheyFailed(expected, actual, byArguments, byCursor);

      if (expected is not null)
      {
        var expectedFailure = Assert.IsType<ShapeException>(expected);
        var actualFailure = Assert.IsType<ShapeException>(actual);

        Assert.Equal(expectedFailure.Message, actualFailure.Message);
        Assert.Equal(expectedFailure.Path, actualFailure.Path);
        Assert.Equal(expectedFailure.Subject, actualFailure.Subject);
        Assert.Equal(expectedFailure.Location.A1, actualFailure.Location.A1);
        return;
      }

      Assert.Equal(byArguments.Value, byCursor.Value);
      Assert.Equal(byArguments.Offset.Size.Width, byCursor.Offset.Size.Width);
      Assert.Equal(byArguments.Offset.Size.Height, byCursor.Offset.Size.Height);
      Assert.Equal(byArguments.Consumed.Width, byCursor.Consumed.Width);
      Assert.Equal(byArguments.Consumed.Height, byCursor.Consumed.Height);
      Assert.Equal(byArguments.Advance.Width, byCursor.Advance.Width);
      Assert.Equal(byArguments.Advance.Height, byCursor.Advance.Height);

      AssertSameDiagnostics(applicative, cursor, space);
    }

    /// <summary>
    /// One spelling throwing while the other does not is the divergence worth catching, and the
    /// least self-explanatory: without this the comparison would go on to type-check a null and
    /// report "value is null" about nothing in particular.
    /// </summary>
    private static void AssertAgreeOnWhetherTheyFailed<T>(
      Exception? applicative,
      Exception? cursor,
      AppliedResult<T> byArguments,
      AppliedResult<T> byCursor)
    {
      if ((applicative is null) == (cursor is null))
        return;

      Assert.Fail(
        applicative is null
          ? $"The cursor spelling threw {Describe(cursor!)}, while the fixed-arity spelling returned <{byArguments.Value}>."
          : $"The fixed-arity spelling threw {Describe(applicative)}, while the cursor spelling returned <{byCursor.Value}>.");
    }

    /// <summary>
    /// The two spellings must notice the same things, not merely produce the same value. This is
    /// the claim that only tooling can tell them apart, so it is compared field by field rather
    /// than through a rendering that could hide a difference.
    /// </summary>
    private static void AssertSameDiagnostics<T>(IShape<T> applicative, IShape<T> cursor, ISpace space)
    {
      var expected = applicative.MapWithDiagnostics(space).Diagnostics;
      var actual = cursor.MapWithDiagnostics(space).Diagnostics;

      Assert.Equal(expected.Count, actual.Count);

      for (var index = 0; index < expected.Count; index++)
      {
        Assert.Equal(expected[index].Severity, actual[index].Severity);
        Assert.Equal(expected[index].Subject, actual[index].Subject);
        Assert.Equal(expected[index].Message, actual[index].Message);
        Assert.Equal(expected[index].Path, actual[index].Path);
        Assert.Equal(expected[index].Location.A1, actual[index].Location.A1);
      }
    }

    private static string Describe(Exception exception)
      => $"{exception.GetType().Name}: {exception.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0]}";

    // --- Leaves ---------------------------------------------------------------------------------

    [Fact]
    public void TwoLeaves()
    {
      AssertIndistinguishable(
        Vertical(IntCell(), IntCell()).Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}"),
        Ladder());
    }

    [Fact]
    public void ChildrenOfDifferentWidths()
    {
      // The cross-axis rule — widest child wins — has to come out the same from both spellings.
      AssertIndistinguishable(
        Vertical(Row(2, r => r.Count), Row(3, r => r.Count)).Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(Row(2, r => r.Count))}|{v.Next(Row(3, r => r.Count))}"),
        CoordinateGrid());
    }

    [Fact]
    public void AHorizontalFlow()
    {
      AssertIndistinguishable(
        Horizontal(IntCell(), IntCell()).Select((first, second) => $"{first}|{second}"),
        Horizontal(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}"),
        CoordinateGrid());
    }

    // --- Composites as children ----------------------------------------------------------------------

    [Fact]
    public void ANestedFlow()
    {
      AssertIndistinguishable(
        Vertical(
          IntCell(),
          Vertical(IntCell(), IntCell()).Select((first, second) => $"({first},{second})"))
          .Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(Vertical(w => $"({w.Next(IntCell())},{w.Next(IntCell())})"))}"),
        Ladder());
    }

    [Fact]
    public void ATableChild()
    {
      var space = Mixed(new object?[,]
      {
        { "Report", null },
        { "Name", "Amount" },
        { "Acme", 1 },
        { "Beta", 2 },
      });

      AssertIndistinguishable(
        Vertical(Cell(c => c.GetString()), TableRows(r => r["Amount"].GetInt()))
          .Select((title, amounts) => $"{title}|{string.Join(",", amounts)}"),
        Vertical(v => $"{v.Next(Cell(c => c.GetString()))}|{string.Join(",", v.Next(TableRows(r => r["Amount"].GetInt())))}"),
        space);
    }

    [Fact]
    public void ARepeatChild()
    {
      AssertIndistinguishable(
        Vertical(IntCell(), Repeat(IntCell()).Select(rest => string.Join(",", rest)))
          .Select((first, rest) => $"{first}|{rest}"),
        Vertical(v => $"{v.Next(IntCell())}|{string.Join(",", v.Next(Repeat(IntCell())))}"),
        Ladder());
    }

    [Fact]
    public void AnOverlayChild()
    {
      AssertIndistinguishable(
        Vertical(
          Overlay(IntCell(), IntCell().Right(2)).Select((left, right) => $"({left},{right})"),
          IntCell())
          .Select((band, next) => $"{band}|{next}"),
        Vertical(v => $"{v.Next(Overlay(IntCell(), IntCell().Right(2)).Select((left, right) => $"({left},{right})"))}|{v.Next(IntCell())}"),
        CoordinateGrid());
    }

    [Fact]
    public void APaddedChild()
    {
      // A pad consumes its insets as well as its content, so the second child's position is a
      // direct read of whether both spellings agree about what the first one took.
      AssertIndistinguishable(
        Vertical(Cells(2, 1, b => b.Width).Padded(1, 0, 0, 0), IntCell()).Select((block, next) => $"{block}|{next}"),
        Vertical(v => $"{v.Next(Cells(2, 1, b => b.Width).Padded(1, 0, 0, 0))}|{v.Next(IntCell())}"),
        CoordinateGrid());
    }

    [Fact]
    public void ASeekAnchoredChild()
    {
      var space = Mixed(new object?[,] { { "preamble" }, { "Section" }, { 7 } });

      AssertIndistinguishable(
        Vertical(
          Cell(c => c.GetString()),
          Cell(c => c.GetString()).After(SeekRowContaining("Section")))
          .Select((first, anchored) => $"{first}|{anchored}"),
        Vertical(v => $"{v.Next(Cell(c => c.GetString()))}|{v.Next(Cell(c => c.GetString()).After(SeekRowContaining("Section")))}"),
        space);
    }

    // --- The whole composite inside something else ------------------------------------------------------

    [Fact]
    public void AFlowRepeatedWithASeparator()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 3 }, { 4 }, { 0 }, { 0 } });

      AssertIndistinguishable(
        Repeat(Vertical(IntCell(), IntCell()).Select((first, second) => $"{first}+{second}"), separatedBy: BlankRows())
          .Select(items => string.Join(" ", items)),
        Repeat(Vertical(v => $"{v.Next(IntCell())}+{v.Next(IntCell())}"), separatedBy: BlankRows())
          .Select(items => string.Join(" ", items)),
        space);
    }

    // --- Failing declarations ------------------------------------------------------------------------------

    [Fact]
    public void FAILING_AChildOfTheWrongKind()
    {
      AssertIndistinguishable(
        Vertical(IntCell(), Cell(c => c.GetString()).Named("title")).Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(Cell(c => c.GetString()).Named("title"))}"),
        Ladder());
    }

    [Fact]
    public void FAILING_AChildThatDoesNotFit()
    {
      AssertIndistinguishable(
        Vertical(Cells(1, 2, b => b.Height), Cells(1, 2, b => b.Height)).Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(Cells(1, 2, b => b.Height))}|{v.Next(Cells(1, 2, b => b.Height))}"),
        Ladder());
    }

    [Fact]
    public void FAILING_ASiblingAfterAnAbsorbedOne()
    {
      // The sibling note lives in the shared FlowState; a re-fork would show up here first.
      var space = Mixed(new object?[,] { { "x" }, { 5 } });

      AssertIndistinguishable(
        Vertical(IntCell().Optional(), IntCell()).Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(IntCell().Optional())}|{v.Next(IntCell())}"),
        space);
    }

    // --- Diagnostics ----------------------------------------------------------------------------------------
    //
    // Every succeeding pair above already compares diagnostics through the shared helper. These
    // three declare shapes whose whole point is to produce some, so the comparison has something
    // substantial to be about.

    [Fact]
    public void AnUnderConsumingFlowLeavesTheSameUnconsumedSpaceInfo()
    {
      // Short on both axes, so the Info has to agree about two counts, two ranges, and the first
      // cell nobody described.
      AssertIndistinguishable(
        Vertical(Row(2, r => r.Count), Row(2, r => r.Count)).Select((first, second) => $"{first}|{second}"),
        Vertical(v => $"{v.Next(Row(2, r => r.Count))}|{v.Next(Row(2, r => r.Count))}"),
        CoordinateGrid());
    }

    [Fact]
    public void AChoiceWhoseLaterAlternativeWinsNamesTheEarlierOneIdentically()
    {
      // The Info for a passed-over alternative carries that alternative's own description and the
      // inner failure's path — both of which are things the two spellings could disagree about.
      AssertIndistinguishable(
        Choice(
          Vertical(Cell(c => c.GetString()), IntCell()).Select((first, second) => $"{first}{second}"),
          Vertical(IntCell(), IntCell()).Select((first, second) => $"{first}|{second}")),
        Choice(
          Vertical(v => $"{v.Next(Cell(c => c.GetString()))}{v.Next(IntCell())}"),
          Vertical(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}")),
        Ladder());
    }

    [Fact]
    public void ABoundaryAbsorbingADeepFailureReportsItIdentically()
    {
      // Three levels down, so the Warning's path is the interesting part: it must name the cell
      // that failed rather than anything that caught it, the same way from both spellings.
      AssertIndistinguishable(
        Vertical(
          IntCell(),
          Vertical(IntCell(), Cell(c => c.GetString()).Named("deep")).Select((first, second) => $"{first}{second}"))
          .Select((first, second) => $"{first}|{second}")
          .Optional(),
        Vertical(v =>
          $"{v.Next(IntCell())}|{v.Next(Vertical(w => $"{w.Next(IntCell())}{w.Next(Cell(c => c.GetString()).Named("deep"))}"))}")
          .Optional(),
        Ladder());
    }

    [Fact]
    public void AnAbsorbedSiblingLeavesTheSameWarningAndTheSameFlow()
    {
      // The absorbed child consumes nothing, so this compares the Warning's content and the
      // position the next child ended up reading, in one declaration.
      AssertIndistinguishable(
        Vertical(Cell(c => c.GetString()).Named("title").Else("fallback"), IntCell())
          .Select((title, next) => $"{title}|{next}"),
        Vertical(v => $"{v.Next(Cell(c => c.GetString()).Named("title").Else("fallback"))}|{v.Next(IntCell())}"),
        Ladder());
    }

    // --- Where the two spellings genuinely differ ---------------------------------------------------------------

    [Fact]
    public void NamingTheCombineIsNotNamingTheFlow()
    {
      // The one visible difference, and it is about the declarations rather than the arithmetic:
      // the applicative spelling has a Select node that the cursor spelling does not, because there
      // the combine IS the lambda. Name the Select and it is the Select that is named — an extra
      // path segment and a different kind. Name the stack, and the two spellings agree exactly.
      var space = Ladder();

      var namedCombine = Vertical(IntCell(), Cell(c => c.GetString()))
        .Select((first, second) => $"{first}{second}")
        .Named("report");

      var namedFlow = Vertical(IntCell(), Cell(c => c.GetString()))
        .Named("report")
        .Select((first, second) => $"{first}{second}");

      var byCursor = Vertical(v => $"{v.Next(IntCell())}{v.Next(Cell(c => c.GetString()))}").Named("report");

      Assert.Equal("'report' -> Vertical -> Cell", Assert.Throws<ShapeException>(() => namedCombine.Map(space)).Path);
      Assert.Equal("'report' -> Cell", Assert.Throws<ShapeException>(() => namedFlow.Map(space)).Path);
      Assert.Equal("'report' -> Cell", Assert.Throws<ShapeException>(() => byCursor.Map(space)).Path);
    }

    // --- Fault classification --------------------------------------------------------------------------------
    //
    // Whether a failure may be absorbed is carried on the exception, but that flag is internal. What
    // a caller can see is whether a boundary swallows it — so the two spellings are compared through
    // one, which is the same question asked from outside.

    [Fact]
    public void ABrokenProjectionIsRefusedByABoundaryInBothSpellings()
    {
      var broken = Cell<string>(_ => throw new NullReferenceException("boom")).Named("broken");

      AssertIndistinguishable(
        Vertical(IntCell(), broken).Select((first, second) => $"{first}|{second}").Optional(),
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(broken)}").Optional(),
        Ladder());
    }

    [Fact]
    public void ADisagreementWithTheDataIsAbsorbedInBothSpellings()
    {
      AssertIndistinguishable(
        Vertical(IntCell(), Cell(c => c.GetString())).Select((first, second) => $"{first}|{second}").Else("absorbed"),
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(Cell(c => c.GetString()))}").Else("absorbed"),
        Ladder());
    }

    [Fact]
    public void FAILING_AFlowThatRanOutOfSpace()
    {
      AssertIndistinguishable(
        Vertical(IntCell(), IntCell(), IntCell(), IntCell()).Select((a, b, c, d) => $"{a}{b}{c}{d}"),
        Vertical(v => $"{v.Next(IntCell())}{v.Next(IntCell())}{v.Next(IntCell())}{v.Next(IntCell())}"),
        Ladder());
    }
  }
}
