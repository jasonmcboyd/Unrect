using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// When a discovered bound goes wrong. A scan that breaks while the projection is consuming it
  /// broke for the same reason it would have broken up front, so it is reported as the <em>
  /// placement's</em> failure and not the projection's: same subject, same path, same cell, same
  /// fault flag, and only the moment differs. Rule 3 of §11.6, which is what lets the differential
  /// suite compare failures at all.
  /// <para>
  /// Two consequences get their own facts here because they are the ones that would hurt. A disk
  /// that stops answering inside a deferred scan must still be a fault — otherwise deferring the
  /// read would be enough to turn a broken file into an absent section under <c>.Optional()</c>. And
  /// a <c>Repeat</c>'s item must never defer at all (rule 2): a repeat stops when its item's
  /// placement fails, and a failure that arrives after the item has been collected is not a stopping
  /// condition, it is an exception.
  /// </para>
  /// </summary>
  public class LazyErrorTimingTests
  {
    private static ISpace Sheet() => Grid(new[,]
    {
      { 1, 2, 3 },
      { 4, 5, 6 },
      { 7, 8, 9 },
      { 0, 0, 0 },
      { 0, 0, 0 },
    });

    /// <summary>
    /// A cell rule that breaks on the cell holding <paramref name="marker"/>. Content-based, never
    /// call-count-based: the two paths read different numbers of cells, so a predicate that counted
    /// its own calls would break in different places and the comparison would be meaningless.
    /// </summary>
    private static Func<CellValue, bool> BreaksOn(int marker)
      => cell => cell.TryGetInt() == marker ? throw new InvalidOperationException("no") : true;

    private static Func<CellValue, bool> FaultsOn(int marker)
      => cell => cell.TryGetInt() == marker ? throw new IOException("the disk stopped answering") : true;

    /// <summary>
    /// The sheet's 7 is the first cell of row 2, so a rule that breaks on it survives the first two
    /// rows — which is what makes it a <em>late</em> break: eagerly the scan reaches it before the
    /// projection starts, lazily only if something asks for row 2 or the engine forces the bound.
    /// </summary>
    private const int LateMarker = 7;

    private static ShapeException Failure<T>(IShape<T> shape, bool eager)
    {
      if (!eager)
        return Assert.Throws<ShapeException>(() => shape.MapWithDiagnostics(Sheet()));

      using (ShapeEngine.ForceEager())
        return Assert.Throws<ShapeException>(() => shape.MapWithDiagnostics(Sheet()));
    }

    private static MapResult<T> Result<T>(IShape<T> shape, bool eager)
    {
      if (!eager)
        return shape.MapWithDiagnostics(Sheet());

      using (ShapeEngine.ForceEager())
        return shape.MapWithDiagnostics(Sheet());
    }

    private static void AssertSameFailureBothWays<T>(IShape<T> shape)
    {
      var deferred = Failure(shape, eager: false);
      var measured = Failure(shape, eager: true);

      Assert.Equal(measured.Message, deferred.Message);
      Assert.Equal(measured.Subject, deferred.Subject);
      Assert.Equal(measured.Path, deferred.Path);
      Assert.Equal(measured.Location.ToString(), deferred.Location.ToString());
      Assert.Equal(measured.IsFault, deferred.IsFault);
      Assert.Equal(measured.InnerException?.GetType(), deferred.InnerException?.GetType());
    }

    // --- A broken scan is the placement's failure, whenever it happens to break --------------------

    [Fact]
    public void AScanThatBreaksImmediatelyFailsIdenticallyBothWays()
    {
      AssertSameFailureBothWays(Range(RowsWhileAny(_ => throw new InvalidOperationException("no")), b => b.Height));
    }

    [Fact]
    public void AScanThatBreaksOnALaterRowFailsIdenticallyBothWays()
    {
      AssertSameFailureBothWays(Range(RowsWhileAny(BreaksOn(LateMarker)), b => b.Height));
    }

    [Fact]
    public void AScanThatBreaksAfterTheProjectionHasFinishedStillFailsIdentically()
    {
      // The genuinely deferred case, and the one rule 3 is written for. The projection reads nothing,
      // so lazily nothing forces the bound until the engine consumes it in full AFTER Project has
      // returned — later than any failure could previously arrive. It still has to be the same one.
      AssertSameFailureBothWays(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0));
    }

    [Fact]
    public void ALateBreakIsReportedAsTheAreaStrategysFailure_NotTheProjections()
    {
      // What "the placement's failure" means concretely: the sentence names the area strategy, even
      // though the break surfaced from inside a space the projection was holding. If this ever read
      // "the projection threw", a declaration would be blamed for its data source's rule.
      var failure = Failure(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0), eager: false);

      Assert.Equal("its area strategy threw InvalidOperationException: no", Problem(failure));
      Assert.Equal("Range", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void ABreakInsideAFlowChildCarriesTheChildsPathBothWays()
    {
      // The path is the thing most likely to drift, because a deferred failure is thrown from a
      // BoundedSpace built at placement time and raised while a different part of the tree is live.
      var shape = VerticalFlow(v =>
      {
        var caption = v.Next(Range(1, 1, b => b[0, 0].GetInt()));
        var body = v.Next(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0).Named("body"));

        return caption + body;
      });

      AssertSameFailureBothWays(shape);

      // The last segment carries the kind alongside the name because the failure is the leaf's own.
      Assert.Equal("VerticalFlow -> 'body' (Range)", Failure(shape, eager: false).Path);
    }

    // --- A fault is never tolerance, however late it arrives ---------------------------------------

    [Theory]
    [InlineData("Optional", false)]
    [InlineData("Optional", true)]
    [InlineData("Else", false)]
    [InlineData("Else", true)]
    [InlineData("Choice", false)]
    [InlineData("Choice", true)]
    public void AnIoFaultInADeferredScanIsNotAbsorbedByAToleranceBoundary(string boundary, bool eager)
    {
      var broken = Range(RowsWhileAny(FaultsOn(LateMarker)), b => b.Height);

      IShape<int> shape = boundary switch
      {
        "Optional" => broken.Optional(),
        "Else" => broken.Else(-1),
        "Choice" => Choice(broken, Range(WholeExtent(), b => b.Height)),

        _ => throw new ArgumentOutOfRangeException(nameof(boundary), boundary, "No such boundary."),
      };

      var failure = Failure(shape, eager);

      Assert.True(failure.IsFault);
      Assert.IsType<IOException>(failure.InnerException);
    }

    [Fact]
    public void AFaultCarriesTheSameIdentityThroughEveryBoundaryBothWays()
    {
      var broken = Range(RowsWhileAny(FaultsOn(LateMarker)), b => b.Height);

      AssertSameFailureBothWays(broken.Optional());
      AssertSameFailureBothWays(broken.Else(-1));
      AssertSameFailureBothWays(Choice(broken, Range(WholeExtent(), b => b.Height)));
    }

    // --- An absorbable break still is absorbable ---------------------------------------------------

    [Theory]
    [InlineData("Optional", false)]
    [InlineData("Optional", true)]
    [InlineData("Else", false)]
    [InlineData("Else", true)]
    public void AnAbsorbableBreakInADeferredScanIsStillAbsorbed(string boundary, bool eager)
    {
      // The other side of the fault rule: deferring must not make a data disagreement harder to
      // tolerate either. The warning names the shape that failed, not the boundary that caught it.
      var broken = Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0);
      var shape = boundary == "Optional" ? broken.Optional() : broken.Else(-1);

      var result = Result(shape, eager);
      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal(boundary == "Optional" ? 0 : -1, result.Value);
      Assert.Equal("its area strategy threw InvalidOperationException: no", warning.Message);
      Assert.Equal("Range", warning.Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AChoiceWhoseFirstAlternativeBreaksLateLeavesNothingBehind(bool eager)
    {
      // Diagnostic rollback under a deferred failure. The losing alternative got as far as being
      // placed and projected before its scan broke, so if the collector were not rewound its
      // near-miss would be joined by whatever it noticed on the way.
      var shape = Choice(
        Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0).Named("first"),
        Range(WholeExtent(), b => b.Height).Named("second"));

      var result = Result(shape, eager);

      Assert.Equal(5, result.Value);

      var note = Assert.Single(result.Diagnostics);
      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Contains("alternative 1", note.Message);
      Assert.Contains("its area strategy threw InvalidOperationException: no", note.Message);
    }

    // --- Rule 2: a repeat's item is placed up front, always ----------------------------------------

    /// <summary>Two blocks of values with one blank row between them, so a repeat finds exactly two.</summary>
    private static ISpace TwoBlocks() => Grid(new[,]
    {
      { 1, 2 },
      { 3, 4 },
      { 5, 6 },
      { 0, 0 },
      { 7, 8 },
      { 9, 10 },
    });

    /// <summary>
    /// How much of the sheet had been read when each item's projection started. Non-zero means the
    /// item's extent was measured before it was projected — which is rule 2, observed.
    /// </summary>
    private static IReadOnlyList<int> RowsReadWhenEachItemsProjectionStarted(bool underRepeat)
    {
      var counter = new CountingSpace(TwoBlocks());
      var observations = new List<int>();

      var item = Range(RowsWhileAnyValue(), _ =>
      {
        observations.Add(counter.RowsTouched);

        return 0;
      });

      if (underRepeat)
        Repeat(item, separatedBy: BlankRows()).Apply(counter);
      else
        item.Apply(counter);

      return observations;
    }

    [Fact]
    public void ARepeatsItemIsMeasuredBeforeItIsProjected()
    {
      // Item one spans rows 0-2 and its scan reads row 3 to learn that it stops, so four rows are
      // behind it when its projection starts; item two adds rows 4 and 5, for six. The same item
      // placed strictly has read nothing at the same moment — which is the whole difference, and the
      // reason a repeat's stopping condition still arrives where it always did.
      Assert.Equal(new[] { 4, 6 }, RowsReadWhenEachItemsProjectionStarted(underRepeat: true));
      Assert.Equal(new[] { 0 }, RowsReadWhenEachItemsProjectionStarted(underRepeat: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARepeatOfDiscoveredItemsStopsRatherThanThrowing(bool eager)
    {
      var repeat = Repeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows());

      IReadOnlyList<int> items;
      if (eager)
      {
        using (ShapeEngine.ForceEager())
          items = repeat.Map(TwoBlocks());
      }
      else
      {
        items = repeat.Map(TwoBlocks());
      }

      // Two blocks, of three rows and two, and then the sheet runs out — a stop, not a failure.
      Assert.Equal(new[] { 3, 2 }, items.ToArray());
    }
  }
}
