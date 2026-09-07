using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A flow composes with every other projection, and this is exactly what that produces. Each test
  /// declares one composition and pins the whole of its outcome: the value, the extent it consumed,
  /// and every diagnostic it left behind — or, where the declaration is wrong, the subject, path,
  /// cell and message of the failure.
  /// <para>
  /// These expectations began as a differential against the fixed-arity spelling, which proved the
  /// two produced byte-identical results. That spelling has since been removed, so the numbers it
  /// agreed on are now written down directly: what they pin is the flow arithmetic in
  /// <c>FlowState</c> and the diagnostics that come out of it.
  /// </para>
  /// </summary>
  public class FlowCompositionTests
  {
    // 4 columns by 3 rows of (row * 10 + column + 1): 1 2 3 4 / 11 12 13 14 / 21 22 23 24.
    /// <summary>
    /// Applies the projection and pins what it read and how much of the space it took. Every
    /// declaration in this file sits at the origin, so its advance is its consumed extent;
    /// asserting both says that the composite added nothing of its own to what its children took.
    /// </summary>
    private static void AssertReads<T>(IProjection<T> projection, ISpace space, T value, int consumedWidth, int consumedHeight)
    {
      var applied = projection.Apply(space);

      Assert.Equal(value, applied.Value);
      Assert.Equal(0, applied.Offset.Size.Width);
      Assert.Equal(0, applied.Offset.Size.Height);
      Assert.Equal(consumedWidth, applied.Consumed.Width);
      Assert.Equal(consumedHeight, applied.Consumed.Height);
      Assert.Equal(consumedWidth, applied.Advance.Width);
      Assert.Equal(consumedHeight, applied.Advance.Height);
    }

    private static void AssertFails<T>(IProjection<T> projection, ISpace space, string subject, string path, string a1, string problem)
    {
      var failure = Assert.Throws<ProjectionException>(() => projection.Map(space));

      Assert.Equal(subject, failure.Subject);
      Assert.Equal(path, failure.Path);
      Assert.Equal(a1, failure.Location.A1);
      Assert.StartsWith($"{subject}: {problem}", failure.Message);
    }

    private static void AssertDiagnostic(
      ProjectionDiagnostic diagnostic,
      DiagnosticSeverity severity,
      string subject,
      string message,
      string path,
      string a1)
    {
      Assert.Equal(severity, diagnostic.Severity);
      Assert.Equal(subject, diagnostic.Subject);
      Assert.Equal(message, diagnostic.Message);
      Assert.Equal(path, diagnostic.Path);
      Assert.Equal(a1, diagnostic.Location.A1);
    }

    private const string WrongKind = "the projection threw InvalidOperationException: Cell value is Number; expected Text.";

    // --- Leaves ---------------------------------------------------------------------------------

    [Fact]
    public void TwoLeaves()
    {
      var projection = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}");

      AssertReads(projection, Ladder(), "1|2", 1, 2);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(Ladder()).Diagnostics),
        DiagnosticSeverity.Info,
        "VerticalFlow",
        "the projection consumed 2 of 3 rows; rows 3+ were not described",
        "VerticalFlow",
        "A3");
    }

    [Fact]
    public void ChildrenOfDifferentWidths()
    {
      // The cross-axis rule: along the axis the children accumulate, across it the widest wins.
      AssertReads(
        VerticalFlow(v => $"{v.Next(Row(2, r => r.Count))}|{v.Next(Row(3, r => r.Count))}"),
        CoordinateGrid(),
        "2|3",
        3,
        2);
    }

    [Fact]
    public void AHorizontalFlow()
    {
      var projection = HorizontalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}");

      AssertReads(projection, CoordinateGrid(), "1|2", 2, 1);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(CoordinateGrid()).Diagnostics),
        DiagnosticSeverity.Info,
        "HorizontalFlow",
        "the projection consumed 1 of 3 rows and 2 of 4 columns; rows 2+ and columns 3+ were not described",
        "HorizontalFlow",
        "C1");
    }

    // --- Composites as children ----------------------------------------------------------------------

    [Fact]
    public void ANestedFlow()
    {
      var projection = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(VerticalFlow(w => $"({w.Next(IntCell())},{w.Next(IntCell())})"))}");

      AssertReads(projection, Ladder(), "1|(2,3)", 1, 3);

      // The whole space described, so nothing to report.
      Assert.Empty(projection.MapWithDiagnostics(Ladder()).Diagnostics);
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

      AssertReads(
        VerticalFlow(v => $"{v.Next(Cell(c => c.GetString()))}|{string.Join(",", v.Next(TableRows(r => r["Amount"].GetInt())))}"),
        space,
        "Report|1,2",
        2,
        4);
    }

    [Fact]
    public void ARepeatChild()
    {
      AssertReads(
        VerticalFlow(v => $"{v.Next(IntCell())}|{string.Join(",", v.Next(VerticalRepeat(IntCell())))}"),
        Ladder(),
        "1|2,3",
        1,
        3);
    }

    [Fact]
    public void AnOverlayChild()
    {
      AssertReads(
        VerticalFlow(v => $"{v.Next(Overlay(o => $"({o.Next(IntCell())},{o.Next(IntCell().Right(2))})"))}|{v.Next(IntCell())}"),
        CoordinateGrid(),
        "(1,3)|11",
        3,
        2);
    }

    [Fact]
    public void APaddedChild()
    {
      // A pad consumes its insets as well as its content, so the second child's position is a
      // direct read of what the first one took.
      AssertReads(
        VerticalFlow(v => $"{v.Next(Range(2, 1, b => b.Width).Padded(1, 0, 0, 0))}|{v.Next(IntCell())}"),
        CoordinateGrid(),
        "2|11",
        3,
        2);
    }

    [Fact]
    public void ASeekAnchoredChild()
    {
      var space = Mixed(new object?[,] { { "preamble" }, { "Section" }, { 7 } });

      AssertReads(
        VerticalFlow(v => $"{v.Next(Cell(c => c.GetString()))}|{v.Next(Cell(c => c.GetString()).On(RowContaining("Section")))}"),
        space,
        "preamble|Section",
        1,
        2);
    }

    // --- The whole composite inside something else ------------------------------------------------------

    [Fact]
    public void AFlowRepeatedWithASeparator()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 3 }, { 4 }, { 0 }, { 0 } });

      var projection = VerticalRepeat(VerticalFlow(v => $"{v.Next(IntCell())}+{v.Next(IntCell())}"), separatedBy: BlankRows())
        .Select(items => string.Join(" ", items));

      AssertReads(projection, space, "1+2 3+4", 1, 5);

      // The trailing blank band is a separator that led nowhere, so the repeat left it unconsumed.
      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(space).Diagnostics),
        DiagnosticSeverity.Info,
        "VerticalRepeat",
        "the projection consumed 5 of 7 rows; rows 6+ were not described",
        "VerticalRepeat",
        "A6");
    }

    // --- Diagnostics ----------------------------------------------------------------------------------------

    [Fact]
    public void AnUnderConsumingFlowSaysWhatItLeft()
    {
      // Short on both axes, so the Info has to name two counts, two ranges, and the first cell
      // nobody described.
      var projection = VerticalFlow(v => $"{v.Next(Row(2, r => r.Count))}|{v.Next(Row(2, r => r.Count))}");

      AssertReads(projection, CoordinateGrid(), "2|2", 2, 2);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(CoordinateGrid()).Diagnostics),
        DiagnosticSeverity.Info,
        "VerticalFlow",
        "the projection consumed 2 of 3 rows and 2 of 4 columns; rows 3+ and columns 3+ were not described",
        "VerticalFlow",
        "C1");
    }

    [Fact]
    public void AChoiceWhoseLaterAlternativeWinsNamesTheEarlierOne()
    {
      var projection = Choice(
        VerticalFlow(v => $"{v.Next(Cell(c => c.GetString()))}{v.Next(IntCell())}"),
        VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}"));

      AssertReads(projection, Ladder(), "1|2", 1, 2);

      var diagnostics = projection.MapWithDiagnostics(Ladder()).Diagnostics;

      Assert.Equal(2, diagnostics.Count);
      AssertDiagnostic(
        diagnostics[0],
        DiagnosticSeverity.Info,
        "Choice",
        $"alternative 1 (VerticalFlow) did not match: {WrongKind}",
        "Choice -> VerticalFlow -> Cell#1",
        "A1");
    }

    [Fact]
    public void ABoundaryAbsorbsADeepFailureAndReportsTheCellThatCausedIt()
    {
      // Three levels down: the Warning must name the cell that failed rather than anything that
      // caught it, and an absorbed projection consumes nothing.
      var projection = VerticalFlow(v =>
        $"{v.Next(IntCell())}|{v.Next(VerticalFlow(w => $"{w.Next(IntCell())}{w.Next(Cell(c => c.GetString()).Named("deep"))}"))}")
        .Optional();

      AssertReads(projection, Ladder(), null, 0, 0);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(Ladder()).Diagnostics),
        DiagnosticSeverity.Warning,
        "'deep'",
        WrongKind,
        "VerticalFlow -> VerticalFlow#2 -> 'deep' (Cell)",
        "A3");
    }

    [Fact]
    public void AnAbsorbedSiblingConsumesNothingAndTheNextChildReadsItsCells()
    {
      var projection = VerticalFlow(v => $"{v.Next(Cell(c => c.GetString()).Named("title").Else("fallback"))}|{v.Next(IntCell())}");

      AssertReads(projection, Ladder(), "fallback|1", 1, 1);

      var diagnostics = projection.MapWithDiagnostics(Ladder()).Diagnostics;

      Assert.Equal(2, diagnostics.Count);
      AssertDiagnostic(diagnostics[0], DiagnosticSeverity.Warning, "'title'", WrongKind, "VerticalFlow -> 'title' (Cell)", "A1");
      AssertDiagnostic(
        diagnostics[1],
        DiagnosticSeverity.Info,
        "VerticalFlow",
        "the projection consumed 1 of 3 rows; rows 2+ were not described",
        "VerticalFlow",
        "A2");
    }

    // --- Naming ------------------------------------------------------------------------------------------------

    [Fact]
    public void NamingAFlowNamesTheFlowItself()
    {
      // A flow has one nameable node, because the combine is the lambda: naming it names the flow,
      // and the failing child follows directly. (The fixed-arity spelling had a second node — the
      // Select that combined the tuple — so naming *it* produced an extra path segment and a
      // '(Select)' kind. Nothing to compare against once that spelling is gone.)
      var projection = VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(Cell(c => c.GetString()))}").Named("report");

      Assert.Equal("'report' -> Cell#2", Assert.Throws<ProjectionException>(() => projection.Map(Ladder())).Path);
    }

    // --- Fault classification --------------------------------------------------------------------------------
    //
    // Whether a failure may be absorbed is carried on the exception, but that flag is internal.
    // What a caller can see is whether a boundary swallows it, which is the same question asked
    // from outside.

    [Fact]
    public void ABrokenProjectionIsRefusedByABoundary()
    {
      AssertFails(
        VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(Cell<string>(_ => throw new NullReferenceException("boom")).Named("broken"))}")
          .Optional(),
        Ladder(),
        "'broken'",
        "VerticalFlow -> 'broken' (Cell)",
        "A2",
        "the projection threw NullReferenceException: boom");
    }

    [Fact]
    public void ADisagreementWithTheDataIsAbsorbed()
    {
      var projection = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(Cell(c => c.GetString()))}").Else("absorbed");

      AssertReads(projection, Ladder(), "absorbed", 0, 0);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(Ladder()).Diagnostics),
        DiagnosticSeverity.Warning,
        "Cell#2",
        WrongKind,
        "VerticalFlow -> Cell#2",
        "A2");
    }

    // --- Failing declarations ------------------------------------------------------------------------------

    [Fact]
    public void FAILING_AChildOfTheWrongKind()
    {
      AssertFails(
        VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(Cell(c => c.GetString()).Named("title"))}"),
        Ladder(),
        "'title'",
        "VerticalFlow -> 'title' (Cell)",
        "A2",
        WrongKind);
    }

    [Fact]
    public void FAILING_AChildThatDoesNotFit()
    {
      AssertFails(
        VerticalFlow(v => $"{v.Next(Range(1, 2, b => b.Height))}|{v.Next(Range(1, 2, b => b.Height))}"),
        Ladder(),
        "Range(1, 2)#2",
        "VerticalFlow -> Range(1, 2)#2",
        "A3",
        "an extent of 1x2 does not fit here");
    }

    [Fact]
    public void FAILING_AFlowThatRanOutOfSpace()
    {
      AssertFails(
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}{v.Next(IntCell())}{v.Next(IntCell())}"),
        Ladder(),
        "Cell#4",
        "VerticalFlow -> Cell#4",
        "A4",
        "an extent of 1x1 does not fit here");
    }

    [Fact]
    public void FAILING_ASiblingAfterAnAbsorbedOne()
    {
      // The sibling note lives in FlowState: a child failing on the very cells its predecessor
      // declined to consume is told why it is probably there.
      AssertFails(
        VerticalFlow(v => $"{v.Next(IntCell().Optional())}|{v.Next(IntCell())}"),
        Mixed(new object?[,] { { "x" }, { 5 } }),
        "Cell#2",
        "VerticalFlow -> Cell#2",
        "A1",
        "the projection threw InvalidOperationException: Cell value is Text; expected Number; "
        + "note: the preceding sibling consumed nothing at this position");
    }
  }
}
