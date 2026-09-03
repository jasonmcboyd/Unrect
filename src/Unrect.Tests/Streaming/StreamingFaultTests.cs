using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Shapes.Shape;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The correctness fix, and the tests that matter most in this feature.
  /// <para>
  /// Under streaming a strategy reads cells. Before this, a disk read failing in the middle of
  /// <c>SkipBlankRows</c> inside <c>section.Optional()</c> was reported as <em>section absent</em>,
  /// with a warning, and the parse carried on and produced a quietly wrong answer. A tolerance
  /// boundary exists to absorb disagreements about the SHAPE of the data; it must never absorb the
  /// environment failing underneath the reader.
  /// </para>
  /// <para>
  /// The controls at the bottom are half the point: if everything were non-absorbable the fault
  /// list would not be a discrimination, it would be a blanket, and <c>.Optional()</c> would mean
  /// nothing.
  /// </para>
  /// </summary>
  public class StreamingFaultTests
  {
    /// <summary>The two exceptions the seam exists to route, as the engine will see them.</summary>
    public static TheoryData<string> Faults => new TheoryData<string> { "io", "disposed" };

    private static Exception Make(string fault) =>
      fault == "io"
        ? new IOException("the disk stopped answering")
        : new ObjectDisposedException("Workbook", "the workbook was disposed under the map");

    /// <summary>
    /// Six rows of readable data whose row <paramref name="faultRow"/> cannot be read.
    /// <para>
    /// One chunk per row, so the failure happens exactly when a declaration reaches that row and
    /// not a moment earlier — which is what makes "this shape got that far" a fact rather than an
    /// inference. A fresh space per call, because the failure is in the load and a store that has
    /// already failed would fail differently the second time.
    /// </para>
    /// </summary>
    private static ISpace Faulting(string fault, int faultRow = 4)
    {
      var source = new FakeRowSource(FakeSheet.Of(
        "Data",
        new object?[] { "Name", "Amount" },
        new object?[] { "a", 1 },
        new object?[] { "b", 2 },
        new object?[] { "c", 3 },
        new object?[] { "d", 4 },
        new object?[] { "e", 5 }))
      {
        Fault = () => Make(fault),
        FaultRow = faultRow
      };

      var pool = new ReaderPool(source, 2, warmReaders: false);

      return new WindowedSpace(new SheetStore(pool, 0, "Data", 6, 2, chunkRows: 1, windowChunks: 4));
    }

    private static void AssertSurfacedAsAFault(string fault, Func<ISpace, object?> map)
    {
      var failure = Assert.Throws<ShapeException>(() => { _ = map(Faulting(fault)); });

      // The original is reachable, so a caller can tell a disk from a disposed workbook and decide
      // whether to retry — which is the whole reason it is wrapped rather than replaced.
      Assert.Equal(fault == "io" ? typeof(IOException) : typeof(ObjectDisposedException), failure.GetBaseException().GetType());
    }

    // --- Where the failure arises ------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Faults))]
    public void AFailureInAProjectionIsAFault(string fault)
    {
      AssertSurfacedAsAFault(fault, space => TableRows(row => row["Amount"].GetInt()).Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void AFailureInAnExtentStrategyIsAFault(string fault)
    {
      // The site the old code got wrong. RowsWhileAnyValue scans rows to decide how tall the region
      // is — reading cells to make a PLACEMENT decision, which the fault flag did not used to cover.
      AssertSurfacedAsAFault(fault, space => Range(block => block.Height).Sized(RowsWhileAnyValue()).Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void AFailureInAnOffsetStrategyIsAFault(string fault)
    {
      // AfterBlankRows scanning into the row that cannot be read: the exact scenario in the spec's
      // statement of the bug.
      AssertSurfacedAsAFault(fault, space => Cell(cell => cell.GetString()).AfterBlankRows().Map(Faulting(fault, faultRow: 0)));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void AFailureInALandmarkIsAFault(string fault)
    {
      // A landmark searches for a row, so it reads its way down the sheet; a search that fails
      // because the disk did is not the same as a search that finished and found nothing.
      AssertSurfacedAsAFault(fault, space => Cell(cell => cell.GetString()).After(To(RowContaining("nowhere"))).Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void AFailureInARepeatSeparatorIsAFault(string fault)
    {
      // The fourth wrapping site, and the one that needed a fault-carrying overload of Failure to
      // reach at all.
      AssertSurfacedAsAFault(fault, space => Repeat(Row(row => row[0].GetString()), separatedBy: BlankRows()).Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void ARepeatCannotEndBecauseTheDiskFailed(string fault)
    {
      // The consequence for a repeat's ITEM. Non-strict placement returns false only for
      // OutOfBoundsException — running out of room is how a repeat stops — so an IO failure inside
      // an item is not a stopping condition and must not be mistaken for the end of the sections.
      AssertSurfacedAsAFault(fault, space => Repeat(Row(row => row[0].GetString())).Map(space));
    }

    // --- What must not absorb it -------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Faults))]
    public void OptionalDoesNotAbsorbAFault(string fault)
    {
      AssertSurfacedAsAFault(fault, space => TableRows(row => row["Amount"].GetInt()).Optional().Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void ElseAValueDoesNotAbsorbAFault(string fault)
    {
      AssertSurfacedAsAFault(
        fault,
        space => TableRows(row => row["Amount"].GetInt()).Else((IReadOnlyList<int>)new[] { 0 }).Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void ElseAFallbackShapeDoesNotAbsorbAFault(string fault)
    {
      AssertSurfacedAsAFault(
        fault,
        space => TableRows(row => row["Amount"].GetInt())
          .Else(Range(block => (IReadOnlyList<int>)new[] { block.Height }))
          .Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void ChoiceDoesNotAbsorbAFault(string fault)
    {
      // A Choice tries its alternatives in turn, rolling back what a failed one consumed. A fault
      // stops the whole thing: the later alternatives are not "what the data might be instead", they
      // are declarations that would read the same broken sheet.
      AssertSurfacedAsAFault(
        fault,
        space => Choice(
          TableRows(row => row["Amount"].GetInt()),
          Range(block => (IReadOnlyList<int>)new[] { block.Height })).Map(space));
    }

    [Theory]
    [MemberData(nameof(Faults))]
    public void AToleratedPlacementFailureIsStillAFault(string fault)
    {
      // The cross that matters most: the failure arises in PLACEMENT and the boundary is one that
      // absorbs placement failures. This is the combination that used to produce a warning and a
      // wrong answer.
      AssertSurfacedAsAFault(
        fault,
        space => Range(block => block.Height).Sized(RowsWhileAnyValue()).Optional().Map(space));

      AssertSurfacedAsAFault(
        fault,
        space => Cell(cell => cell.GetString()).After(To(RowContaining("nowhere"))).Optional().Map(space));

      AssertSurfacedAsAFault(
        fault,
        space => Repeat(Row(row => row[0].GetString()), separatedBy: BlankRows()).Optional().Map(space));
    }

    [Fact]
    public void AFaultIsNotReportedAsADiagnostic()
    {
      // MapWithDiagnostics is where an absorbed failure would show up as a Warning saying the
      // section was absent. It must throw instead: there is no diagnostic that can honestly describe
      // a sheet nobody could read.
      var declaration = TableRows(row => row["Amount"].GetInt()).Named("amounts").Optional();

      Assert.Throws<ShapeException>(() => declaration.MapWithDiagnostics(Faulting("io")));
    }

    [Fact]
    public void AFaultNamesTheShapeAndTheCell()
    {
      // A fault is still a ShapeException, which means it still says which declaration was reading
      // and where. "The disk failed" without that is a stack trace; with it, it is a bug report.
      var failure = Assert.Throws<ShapeException>(
        () => TableRows(row => row["Amount"].GetInt()).Named("amounts").Map(Faulting("io")));

      Assert.Contains("'amounts'", failure.Message);
      Assert.Contains("IOException", failure.Message);
      Assert.Contains("the disk stopped answering", failure.Message);
    }

    // --- The controls: what a boundary IS still for --------------------------------------------------

    private static ISpace Sound() => ShapeTestSpaces.Mixed(new object?[,]
    {
      { "Name", "Amount" },
      { "a", 1 },
      { "b", 2 },
    });

    [Fact]
    public void ACellOfTheWrongKindIsStillAbsorbed()
    {
      // "Amount is text where a number was declared" is a disagreement about the data, which is
      // exactly what a tolerance boundary is for.
      var value = Cell(cell => cell.GetInt()).Optional().Map(Sound());

      Assert.Equal(0, value);
    }

    [Fact]
    public void AMissingAnchorIsStillAbsorbed()
    {
      var value = Cell(cell => cell.GetString()).After(To(RowContaining("absent"))).Optional().Map(Sound());

      Assert.Null(value);
    }

    [Fact]
    public void AnExtentLargerThanTheSpaceIsStillAbsorbed()
    {
      var value = Range(block => block.Height).Sized(Extent(9, 9)).Optional().Map(Sound());

      Assert.Equal(0, value);
    }

    [Fact]
    public void ARepeatStillStopsAtTheEndOfItsSections()
    {
      // Running out of room is how a repeat ends, and no part of the fault work may change that.
      var rows = Repeat(Row(row => row[0].GetString())).Map(Sound());

      Assert.Equal(new[] { "Name", "a", "b" }, rows.ToArray());
    }

    [Fact]
    public void AChoiceStillFallsBackWhenTheDataDisagrees()
    {
      var value = Choice(
        Cell(cell => cell.GetInt()).Named("a number"),
        Cell(cell => cell.GetString().Length).Named("its length")).Map(Sound());

      Assert.Equal(4, value);
    }
  }
}
