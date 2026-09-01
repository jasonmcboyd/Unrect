using System;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// A flow whose children are declared by calling <c>Next</c> rather than by being passed in. The
  /// order of the calls is the order of the children, and nothing else about the lambda is a
  /// declaration — there is deliberately no way to ask the cursor where it is or how much is left.
  /// <para>
  /// The arithmetic these tests exercise is shared with the fixed-arity spelling and pinned as
  /// identical in <see cref="CursorStackDifferentialTests"/>; what is here is the behaviour that
  /// belongs to this spelling alone — misuse of the cursor, faults raised by the lambda itself, and
  /// the opacity that is the whole cost of the experiment.
  /// </para>
  /// </summary>
  public class CursorStackShapeTests
  {
    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    private static IShape<string> Text() => Cell(v => v.GetString());

    private static ISpace Ladder(int height = 3)
    {
      var values = new int[height, 1];

      for (var row = 0; row < height; row++)
        values[row, 0] = row + 1;

      return Grid(values);
    }

    private static ISpace CoordinateGrid()
    {
      var values = new int[3, 4];

      for (var row = 0; row < 3; row++)
        for (var column = 0; column < 4; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    // --- Flow arithmetic ---------------------------------------------------------------------------

    [Fact]
    public void TheOrderOfTheNextCallsIsTheOrderOfTheChildren()
    {
      Assert.Equal("1|2|3", Vertical(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}|{v.Next(IntCell())}").Map(Ladder()));
    }

    [Fact]
    public void AFlowAdvancesAlongItsOwnAxisOnly()
    {
      // The first child is inset one column; the second starts back at column 0, one row down.
      Assert.Equal("2|3", Vertical(v => $"{v.Next(IntCell().Right(1))}|{v.Next(IntCell())}").Map(Grid(new[,] { { 1, 2 }, { 3, 4 } })));
    }

    [Fact]
    public void AHorizontalFlowAdvancesAcrossOnly()
    {
      Assert.Equal("3|2", Horizontal(v => $"{v.Next(IntCell().Down(1))}|{v.Next(IntCell())}").Map(Grid(new[,] { { 1, 2 }, { 3, 4 } })));
    }

    [Fact]
    public void AFlowIsAsWideAsItsWidestChild()
    {
      var applied = Vertical(v => $"{v.Next(Row(2, r => r.Count))}|{v.Next(Row(3, r => r.Count))}").Apply(CoordinateGrid());

      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void AChildWithADeclaredArea_IsConsumedInFull()
    {
      // The block only reads two rows because it was told to; the next child starts after them.
      Assert.Equal("2|3", Vertical(v => $"{v.Next(Cells(1, 2, b => b.Height))}|{v.Next(IntCell())}").Map(Ladder()));
    }

    [Fact]
    public void Sized_OverridesWhatTheFlowDerived()
    {
      var applied = Vertical(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}")
        .Sized(AreaStrategies.ExplicitArea(1, 3))
        .Apply(Ladder());

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void ASingleNextCallIsALegalFlow()
    {
      // One child is a Select with a placement, which is a useful thing to declare.
      var applied = Vertical(v => v.Next(IntCell())).Down(1).Apply(Ladder());

      Assert.Equal(2, applied.Value);
      Assert.Equal(1, applied.Consumed.Height);
    }

    // --- The sibling note ---------------------------------------------------------------------------

    [Fact]
    public void AFailureAfterASiblingThatConsumedNothing_IsNoted()
    {
      var space = Mixed(new object?[,] { { "x" }, { 5 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(v => $"{v.Next(IntCell().Optional())}|{v.Next(IntCell())}").Map(space));

      Assert.Contains("note: the preceding sibling consumed nothing at this position", failure.Message);
    }

    [Fact]
    public void AChildThatReAnchoredItselfAndFailedElsewhere_IsNotNoted()
    {
      var space = Mixed(new object?[,] { { "x" }, { null }, { null }, { 5 }, { 6 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(v => $"{v.Next(IntCell().Optional())}|{v.Next(Text().After(BlankRows()).Down(2))}").Map(space));

      Assert.DoesNotContain("note:", failure.Message);
      Assert.Equal("A3", failure.Location.A1);
    }

    // --- Repetition ------------------------------------------------------------------------------------

    [Fact]
    public void ARepeatedFlow_StopsAtTheSeparator()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 3 }, { 4 }, { 0 }, { 0 } });

      var items = Repeat(Vertical(v => $"{v.Next(IntCell())}+{v.Next(IntCell())}"), separatedBy: BlankRows()).Map(space);

      Assert.Equal(new[] { "1+2", "3+4" }, items);
    }

    [Fact]
    public void ARepeatedFlow_HonoursAtLeast()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 3 }, { 4 }, { 0 }, { 0 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Repeat(Vertical(v => $"{v.Next(IntCell())}+{v.Next(IntCell())}"), separatedBy: BlankRows(), atLeast: 5).Map(space));

      Assert.Contains("expected at least 5 occurrences but found 2", failure.Message);
    }

    [Fact]
    public void AFailureInsideARepeatedFlow_IsLoudRatherThanAStop()
    {
      // A Next call is deeper than the item's own placement, so it is drift, not the end of the run.
      var failure = Assert.Throws<ShapeException>(() =>
        Repeat(Vertical(v => $"{v.Next(IntCell())}+{v.Next(Text())}")).Map(Ladder()));

      Assert.Contains("Repeat[0]", failure.Path);
      Assert.Contains("expected Text", failure.Message);
    }

    [Fact]
    public void ARepeatedFlowThatConsumesNothing_Terminates()
    {
      Assert.Empty(Repeat(Vertical(v => v.Next(Cells(AreaStrategies.MinArea(), b => b.Width)))).Map(Ladder()));
    }

    // --- Alternation -----------------------------------------------------------------------------------

    [Fact]
    public void ALosingBranchLeavesNoDiagnosticsButDoesRunItsLambdaPartially()
    {
      // The combine IS the lambda here, so a losing branch runs every expression up to and
      // including the failing Next and no further. Diagnostics roll back; side effects do not —
      // which is the whole reason the factory's documentation says to capture nothing you write to.
      var reached = 0;

      var losing = Vertical(v =>
      {
        var first = v.Next(IntCell());
        reached++;
        return $"{first}{v.Next(Text())}";
      });

      var result = Choice(losing, Vertical(v => $"{v.Next(IntCell())}w")).MapWithDiagnostics(Ladder());

      Assert.Equal("1w", result.Value);
      Assert.Equal(1, reached);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void AWinningBranchNamesTheOnesBeforeIt()
    {
      var result = Choice(
        Vertical(v => $"{v.Next(Text())}"),
        Vertical(v => $"{v.Next(IntCell())}"))
        .MapWithDiagnostics(Ladder());

      Assert.Equal("1", result.Value);
      Assert.Contains(
        result.Diagnostics,
        d => d.Severity == DiagnosticSeverity.Info && d.Message.StartsWith("alternative 1 (Vertical) did not match: "));
    }

    // --- Tolerance -------------------------------------------------------------------------------------

    [Fact]
    public void ABoundaryAroundAFlow_AbsorbsADeepFailureWithTheInnerPath()
    {
      var deep = Vertical(v =>
        $"{v.Next(IntCell())}|{v.Next(Vertical(w => $"{w.Next(IntCell())}{w.Next(Text().Named("deep"))}"))}");

      var result = deep.Optional().MapWithDiagnostics(Ladder());

      Assert.Null(result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Equal("Vertical -> Vertical -> 'deep' (Cell)", warning.Path);
      Assert.Equal("A3", warning.Location.A1);
    }

    [Fact]
    public void AnAbsorbedFlow_ConsumesNothing()
    {
      var applied = Vertical(v => $"{v.Next(Vertical(w => w.Next(Text())).Else("fallback"))}|{v.Next(IntCell())}")
        .Apply(Ladder());

      Assert.Equal("fallback|1", applied.Value);
      Assert.Equal(1, applied.Consumed.Height);
    }

    // --- Faults ------------------------------------------------------------------------------------------

    [Fact]
    public void AChildFailureInsideNext_IsNotWrappedAgain()
    {
      // The failure belongs to the child, with the child's path and cell. Only the sibling note may
      // ever be added to it.
      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(Text().Named("title"))}").Map(Ladder()));

      Assert.Equal("'title'", failure.Subject);
      Assert.Equal("Vertical -> 'title' (Cell)", failure.Path);
      Assert.Equal("A2", failure.Location.A1);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public void UserCodeThrowingBetweenNextCalls_IsWrappedOnceAtTheFlowsOwnOrigin()
    {
      // The throw happened in the outer lambda, so the flow is what failed. This is why parsing
      // belongs inside a leaf's projection, where the location is the cell.
      var failure = Assert.Throws<ShapeException>(() =>
        Vertical<int>(v => { _ = v.Next(IntCell()); throw new InvalidOperationException("boom"); }).Map(Ladder()));

      Assert.Equal("Vertical", failure.Subject);
      Assert.Equal("Vertical", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
      Assert.Contains("the projection threw InvalidOperationException: boom", failure.Message);
    }

    [Fact]
    public void ANullReferenceBetweenNextCalls_IsAFaultAndIsNotAbsorbed()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Vertical<int>(v => { _ = v.Next(IntCell()); throw new NullReferenceException("boom"); })
          .Optional()
          .Map(Ladder()));

      Assert.IsType<NullReferenceException>(failure.GetBaseException());
    }

    [Fact]
    public void AnArgumentExceptionBetweenNextCalls_IsStillAbsorbable()
    {
      // A parse that disagreed with the data is what tolerance is for; only the broken-code
      // exceptions are exempt.
      var result = Vertical<int>(v => { _ = v.Next(IntCell()); throw new ArgumentException("bad"); })
        .Optional()
        .MapWithDiagnostics(Ladder());

      Assert.Equal(0, result.Value);
      Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    // --- Misuse -----------------------------------------------------------------------------------------------

    [Fact]
    public void AFlowThatDeclaresNothing_Fails()
    {
      // It would match anything, describe nothing, and quietly end an enclosing repetition.
      var failure = Assert.Throws<ShapeException>(() => Vertical(_ => 42).Map(Ladder()));

      Assert.Contains("a flow must declare at least one shape; this one called Next zero times", failure.Message);
    }

    [Fact]
    public void AFlowThatDeclaresNothing_ResistsAToleranceBoundary()
    {
      // A declaration bug is not a shape of data, so no boundary may hide it.
      Assert.Throws<ShapeException>(() => Vertical(_ => 42).Optional().Map(Ladder()));
    }

    [Fact]
    public void ANullShapeIsReportedWhereTheChildWouldHaveGone()
    {
      // The position is the pin: reporting the composite's own origin would be correct but vaguer,
      // and A2 is only right because the context is advanced to the cursor before it is blamed.
      IShape<int>? missing = null;

      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(missing!)}").Map(Ladder()));

      Assert.Contains("a null shape was declared as child 2", failure.Message);
      Assert.Equal("A2", failure.Location.A1);
      Assert.Equal("Vertical", failure.Path);
    }

    [Fact]
    public void ANullFirstChildIsReportedAtTheFlowsOrigin()
    {
      IShape<int>? missing = null;

      var failure = Assert.Throws<ShapeException>(() => Vertical(v => $"{v.Next(missing!)}").Map(Ladder()));

      Assert.Contains("a null shape was declared as child 1", failure.Message);
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void ANullShape_ResistsAToleranceBoundary()
    {
      IShape<int>? missing = null;

      Assert.Throws<ShapeException>(() =>
        Vertical(v => $"{v.Next(IntCell())}|{v.Next(missing!)}").Optional().Map(Ladder()));
    }

    [Fact]
    public void ACursorThatNeverHadALayout_RefusesToBeUsed()
    {
      // The only escape the compiler cannot catch, because anyone can construct it. The message
      // says which of the two ways of being outside a layout this is; the other — a cursor used
      // after its layout returned — cannot be reached from C# at all, so it has no test.
      var failure = Assert.Throws<InvalidOperationException>(() => default(LayoutCursor).Next(IntCell()));

      Assert.Equal(
        "A layout cursor cannot be used outside the layout that created it; this one never had a layout.",
        failure.Message);
    }

    // --- Inspection -------------------------------------------------------------------------------------------------

    [Fact]
    public void AFlowDescribesItselfExactlyAsTheFixedAritySpellingDoes()
    {
      // Diagnostics must not fork on spelling, so the description cannot either.
      Assert.Equal(
        Vertical(IntCell(), IntCell()).Description,
        Vertical(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Description);

      Assert.Equal(
        Horizontal(IntCell(), IntCell()).Description,
        Horizontal(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Description);
    }

    [Fact]
    public void AFlowHasNoChildrenToEnumerate()
    {
      // The cost of the experiment, stated as a test: what it declares is knowable only by running
      // it, so anything structural that runs without a space sees nothing.
      Assert.Empty(Vertical(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Children);
    }

    [Fact]
    public void AFlowSaysWhyItsChildrenAreMissing()
    {
      // Empty children would read as "leaf" to a renderer, which is a lie; this is how it can tell.
      // The marker is internal, so this reaches it the way tooling in another assembly could not.
      var shape = Vertical(v => $"{v.Next(IntCell())}{v.Next(IntCell())}");

      var marker = shape.GetType().GetInterface("Unrect.Shapes.IOpaqueComposite");

      Assert.NotNull(marker);
      Assert.Equal(
        "declared by a cursor lambda; children are known only while it runs",
        marker!.GetProperty("Reason")!.GetValue(shape));
    }

    [Fact]
    public void TheFixedAritySpellingIsNotOpaque()
    {
      Assert.Null(Vertical(IntCell(), IntCell()).GetType().GetInterface("Unrect.Shapes.IOpaqueComposite"));
    }

    [Fact]
    public void AFlowIsAShapeAndCanBeNamedAndPlaced()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 } });

      var shape = Vertical(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}").AfterBlankRows().Named("block");

      Assert.Equal("1|2", shape.Map(space));
      Assert.Equal("block", shape.Name);
      Assert.False(shape.IsTransparent);
      Assert.Null(shape.Placement.Area);
    }

    [Fact]
    public void ACaptureNothingFlowIsSafeToApplyToManySpacesAtOnce()
    {
      // The immutability guarantee survives the lambda as long as the lambda writes to nothing.
      var shape = Vertical(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}");

      var spaces = Enumerable.Range(0, 64)
        .Select(seed => Grid(new[,] { { seed + 1 }, { seed + 2 } }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index => results[index] = shape.Map(spaces[index]));

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"{index + 1}|{index + 2}", results[index]);
    }

    // --- Escape hazards: compiler diagnostics, not assertions ------------------------------------------------------------
    //
    // LayoutCursor is a readonly ref struct, so every way of using it outside its own lambda is a
    // compile error rather than a runtime one. That is the guard, and it cannot be written as a
    // test without a compilation harness — so the snippets live here, each verified against this
    // build to produce the code shown.
    //
    //   IShape<List<int>> s = Vertical(v => Enumerable.Range(0, 3).Select(i => v.Next(x)).ToList());
    //     CS9108 — cannot use ref-like 'v' inside an anonymous method or lambda. Covers the
    //     deferred-query hazard too: unmaterialised, the query fails the same way.
    //
    //   IShape<int> s = Vertical(v => { int F() => v.Next(x); return F(); });
    //     CS9108 — the same rule for a local function.
    //
    //   IShape<LayoutCursor> s = Vertical(v => v);
    //     CS9244 — the type 'LayoutCursor' may not be a type argument (returning the cursor).
    //
    //   static LayoutCursor field; ... Vertical(v => { field = v; return v.Next(x); });
    //     CS8345 — a ref-struct field may not be a member of a class.
    //
    //   Vertical(v => { var list = new List<LayoutCursor>(); list.Add(v); return v.Next(x); });
    //     CS9244 — the generic argument fails before the Add does.
    //
    //   Vertical(v => { var array = new LayoutCursor[1]; array[0] = v; return v.Next(x); });
    //     CS0611 — an array element may not be a ref struct.
    //
    //   Vertical(header);   // one shape, no lambda
    //     CS0411 — no silent binding to an unintended overload.
    //
    // The one escape the compiler cannot see is default(LayoutCursor), which anyone can construct;
    // ACursorThatNeverHadALayout_RefusesToBeUsed above is that guard.
  }
}
