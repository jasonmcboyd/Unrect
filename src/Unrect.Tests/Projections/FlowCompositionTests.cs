using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
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
    private static void AssertReads<T>(IProjectionDefinition<ISheetCells, T> projection, ISheetCells space, T value, int consumedWidth, int consumedHeight)
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

    private static void AssertFails<T>(IProjectionDefinition<ISheetCells, T> projection, ISheetCells space, string subject, string path, string a1, string problem)
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

    /// <summary>
    /// A Text leaf refusing a Number cell, as the diagnostic reads — and the A1 is now part of the
    /// sentence, which is why this is a method rather than the constant it used to be.
    /// <para>
    /// Until phase 6 the leaf threw an InvalidOperationException from inside its own lambda and the
    /// engine wrapped it: <c>the projection threw InvalidOperationException: Cell value is Number;
    /// expected Text.</c> — the reader's vocabulary around the document's, with the location left to
    /// the diagnostic's own A1 field. The read failure is a first-class condition now, so what is
    /// reported is the backend's sentence, addressed, and nothing else.
    /// </para>
    /// </summary>
    private static string WrongKind(string at) => $"expected Text at {at}, found Number";

    // --- Leaves ---------------------------------------------------------------------------------

    [Fact]
    public void TwoLeaves()
    {
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      });

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
        VerticalFlow(v =>
        {
          var rowSlot = v.Next(Row(2, r => r.Count));
          var rowSlot2 = v.Next(Row(3, r => r.Count));

          return v.Build(read => $"{read.Of(rowSlot)}|{read.Of(rowSlot2)}");
        }),
        CoordinateGrid(),
        "2|3",
        3,
        2);
    }

    [Fact]
    public void AHorizontalFlow()
    {
      var projection = HorizontalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      });

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
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var verticalFlow = v.Next(VerticalFlow(w =>
        {
          var intCell = w.Next(IntCell());
          var intCell2 = w.Next(IntCell());

          return w.Build(read => $"({read.Of(intCell)},{read.Of(intCell2)})");
        }));

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(verticalFlow)}");
      });

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
        VerticalFlow(v =>
        {
          var textCell = v.Next(TextCell());
          var table = v.Next(Table(r => r["Amount"].Integer()));

          return v.Build(read => $"{read.Of(textCell)}|{string.Join(",", read.Of(table))}");
        }),
        space,
        "Report|1,2",
        2,
        4);
    }

    [Fact]
    public void ARepeatChild()
    {
      AssertReads(
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var verticalRepeat = v.Next(VerticalRepeat(IntCell()));

          return v.Build(read => $"{read.Of(intCell)}|{string.Join(",", read.Of(verticalRepeat))}");
        }),
        Ladder(),
        "1|2,3",
        1,
        3);
    }

    [Fact]
    public void AnOverlayChild()
    {
      AssertReads(
        VerticalFlow(v =>
        {
          var overlay = v.Next(Overlay(o =>
          {
            var intCell = o.Next(IntCell());
            var right = o.Next(Right(2).Of(IntCell()));

            return o.Build(read => $"({read.Of(intCell)},{read.Of(right)})");
          }));
          var intCell = v.Next(IntCell());

          return v.Build(read => $"{read.Of(overlay)}|{read.Of(intCell)}");
        }),
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
        VerticalFlow(v =>
        {
          var rangeSlot = v.Next(Range(2, 1, b => b.Width).Padded(1, 0, 0, 0));
          var intCell = v.Next(IntCell());

          return v.Build(read => $"{read.Of(rangeSlot)}|{read.Of(intCell)}");
        }),
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
        VerticalFlow(v =>
        {
          var textCell = v.Next(TextCell());
          var on = v.Next(On(RowContaining("Section")).Of(TextCell()));

          return v.Build(read => $"{read.Of(textCell)}|{read.Of(on)}");
        }),
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

      var projection = VerticalRepeat(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}+{read.Of(intCell2)}");
      }), separatedBy: BlankRows())
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
      var projection = VerticalFlow(v =>
      {
        var rowSlot = v.Next(Row(2, r => r.Count));
        var rowSlot2 = v.Next(Row(2, r => r.Count));

        return v.Build(read => $"{read.Of(rowSlot)}|{read.Of(rowSlot2)}");
      });

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
        VerticalFlow(v =>
        {
          var textCell = v.Next(TextCell());
          var intCell = v.Next(IntCell());

          return v.Build(read => $"{read.Of(textCell)}{read.Of(intCell)}");
        }),
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var intCell2 = v.Next(IntCell());

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
        }));

      AssertReads(projection, Ladder(), "1|2", 1, 2);

      var diagnostics = projection.MapWithDiagnostics(Ladder()).Diagnostics;

      Assert.Equal(2, diagnostics.Count);
      AssertDiagnostic(
        diagnostics[0],
        DiagnosticSeverity.Info,
        "Choice",
        $"alternative 1 (VerticalFlow) did not match: {WrongKind("A1")}",
        "Choice -> VerticalFlow -> Text#1",
        "A1");
    }

    [Fact]
    public void ABoundaryAbsorbsADeepFailureAndReportsTheCellThatCausedIt()
    {
      // Three levels down: the Warning must name the cell that failed rather than anything that
      // caught it, and an absorbed projection consumes nothing.
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var verticalFlow = v.Next(VerticalFlow(w =>
          {
            var intCell = w.Next(IntCell());
            var textCell = w.Next(TextCell().Named("deep"));

            return w.Build(read => $"{read.Of(intCell)}{read.Of(textCell)}");
          }));

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(verticalFlow)}");
      })
        .Optional();

      AssertReads(projection, Ladder(), null, 0, 0);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(Ladder()).Diagnostics),
        DiagnosticSeverity.Warning,
        "'deep'",
        WrongKind("A3"),
        "VerticalFlow -> VerticalFlow#2 -> 'deep' (Text)",
        "A3");
    }

    [Fact]
    public void AnAbsorbedSiblingConsumesNothingAndTheNextChildReadsItsCells()
    {
      var projection = VerticalFlow(v =>
      {
        var textCell = v.Next(TextCell().Named("title").Else("fallback"));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(textCell)}|{read.Of(intCell)}");
      });

      AssertReads(projection, Ladder(), "fallback|1", 1, 1);

      var diagnostics = projection.MapWithDiagnostics(Ladder()).Diagnostics;

      Assert.Equal(2, diagnostics.Count);
      AssertDiagnostic(diagnostics[0], DiagnosticSeverity.Warning, "'title'", WrongKind("A1"), "VerticalFlow -> 'title' (Text)", "A1");
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
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var textCell = v.Next(TextCell());

        return v.Build(read => $"{read.Of(intCell)}{read.Of(textCell)}");
      }).Named("report");

      Assert.Equal("'report' -> Text#2", Assert.Throws<ProjectionException>(() => projection.Map(Ladder())).Path);
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
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var pointSlot = v.Next(Point().Select<ISheetCells, Point<ISheetCells>, string>(_ => throw new NullReferenceException("boom")).Named("broken"));

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(pointSlot)}");
        })
          .Optional(),
        Ladder(),
        "'broken'",
        "VerticalFlow -> 'broken' (Select)",
        "A2",
        "the projection threw NullReferenceException: boom");
    }

    [Fact]
    public void ADisagreementWithTheDataIsAbsorbed()
    {
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var textCell = v.Next(TextCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(textCell)}");
      }).Else("absorbed");

      AssertReads(projection, Ladder(), "absorbed", 0, 0);

      AssertDiagnostic(
        Assert.Single(projection.MapWithDiagnostics(Ladder()).Diagnostics),
        DiagnosticSeverity.Warning,
        "Text#2",
        WrongKind("A2"),
        "VerticalFlow -> Text#2",
        "A2");
    }

    // --- Failing declarations ------------------------------------------------------------------------------

    [Fact]
    public void FAILING_AChildOfTheWrongKind()
    {
      AssertFails(
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var textCell = v.Next(TextCell().Named("title"));

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(textCell)}");
        }),
        Ladder(),
        "'title'",
        "VerticalFlow -> 'title' (Text)",
        "A2",
        WrongKind("A2"));
    }

    [Fact]
    public void FAILING_AChildThatDoesNotFit()
    {
      AssertFails(
        VerticalFlow(v =>
        {
          var rangeSlot = v.Next(Range(1, 2, b => b.Height));
          var rangeSlot2 = v.Next(Range(1, 2, b => b.Height));

          return v.Build(read => $"{read.Of(rangeSlot)}|{read.Of(rangeSlot2)}");
        }),
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
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var intCell2 = v.Next(IntCell());
          var intCell3 = v.Next(IntCell());
          var intCell4 = v.Next(IntCell());

          return v.Build(read => $"{read.Of(intCell)}{read.Of(intCell2)}{read.Of(intCell3)}{read.Of(intCell4)}");
        }),
        Ladder(),
        "Integer#4",
        "VerticalFlow -> Integer#4",
        "A4",
        "an extent of 1x1 does not fit here");
    }

    [Fact]
    public void FAILING_ASiblingAfterAnAbsorbedOne()
    {
      // The sibling note lives in FlowState: a child failing on the very cells its predecessor
      // declined to consume is told why it is probably there.
      AssertFails(
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell().Optional());
          var intCell2 = v.Next(IntCell());

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
        }),
        Mixed(new object?[,] { { "x" }, { 5 } }),
        "Integer#2",
        "VerticalFlow -> Integer#2",
        "A1",
        "expected Number at A1, found Text; "
        + "note: the preceding sibling consumed nothing at this position");
    }
  }
}
