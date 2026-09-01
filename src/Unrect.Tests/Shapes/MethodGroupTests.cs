using System;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// One shape applied over many spaces, written as a method group. This is the founding use case —
  /// a declaration is a value, and <c>spaces.Select(report.Map)</c> is what that buys — and it is
  /// fragile in a way nothing else in the suite would catch.
  /// <para>
  /// <strong>Why these tests exist.</strong> A method group cannot be converted to a delegate that
  /// omits an optional parameter (<c>CS0123</c>). So adding <em>any</em> optional parameter to
  /// <c>Map</c>, <c>Apply</c>, or <c>MapWithDiagnostics</c> — a <c>CallerArgumentExpression</c> for
  /// naming the root, most plausibly — silently breaks every caller written this way, while every
  /// direct call site keeps compiling. That exact change was made once and reverted (see the
  /// invertibility audit §4.8 and <c>Map</c>'s XML doc). These pins turn the next attempt into a
  /// test failure at the point of change rather than a discovery in someone's script.
  /// </para>
  /// </summary>
  public class MethodGroupTests
  {
    private static IShape<int> Report() => VerticalFlow(v => v.Next(Cell(c => c.GetInt())));

    private static ISpace[] Workbooks() => new[]
    {
      Grid(new[,] { { 1 } }),
      Grid(new[,] { { 2 } }),
      Grid(new[,] { { 3 } }),
    };

    [Fact]
    public void Map_ConvertsToADelegateAsAMethodGroup()
    {
      // If this stops compiling, Map has grown a parameter and the idiom below is already broken.
      Func<ISpace, int> parse = Report().Map;

      Assert.Equal(1, parse(Workbooks()[0]));
    }

    [Fact]
    public void Map_IsUsableAsAProjectionOverManySpaces()
    {
      // The shape of the founding use case: declare once, apply to a directory of workbooks.
      var report = Report();

      Assert.Equal(new[] { 1, 2, 3 }, Workbooks().Select(report.Map).ToArray());
    }

    [Fact]
    public void Apply_ConvertsToADelegateAsAMethodGroup()
    {
      Func<ISpace, AppliedResult<int>> apply = Report().Apply;

      var applied = apply(Workbooks()[1]);

      Assert.Equal(2, applied.Value);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void MapWithDiagnostics_ConvertsToADelegateAsAMethodGroup()
    {
      Func<ISpace, MapResult<int>> read = Report().MapWithDiagnostics;

      var result = read(Workbooks()[2]);

      Assert.Equal(3, result.Value);
      Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void AllThreeEntryPointsAgreeWhenCalledThroughDelegates()
    {
      var report = Report();
      var space = Workbooks()[0];

      Func<ISpace, int> map = report.Map;
      Func<ISpace, AppliedResult<int>> apply = report.Apply;
      Func<ISpace, MapResult<int>> diagnose = report.MapWithDiagnostics;

      Assert.Equal(map(space), apply(space).Value);
      Assert.Equal(map(space), diagnose(space).Value);
    }

    // --- The root's own name ---------------------------------------------------------------------

    [Fact]
    public void TheRootOfAPathRendersByItsDescription()
    {
      // The other half of the same decision: naming the root from the receiver would need the
      // optional parameter the tests above forbid, so the root renders structurally instead.
      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => v.Next(Cell(c => c.GetString()))).Map(Grid(new[,] { { 1 } })));

      Assert.Equal("VerticalFlow -> Cell#1", failure.Path);
    }

    [Fact]
    public void ARootWorthNamingIsNamedExplicitly()
    {
      // ...and Named is the mechanism, unchanged. A root worth a name is worth writing one.
      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => v.Next(Cell(c => c.GetString()))).Named("report").Map(Grid(new[,] { { 1 } })));

      Assert.Equal("'report' -> Cell#1", failure.Path);
    }

    [Fact]
    public void ANamedRootStillConvertsToADelegate()
    {
      // Naming the root must not cost the idiom either.
      Func<ISpace, int> parse = Report().Named("report").Map;

      Assert.Equal(new[] { 1, 2, 3 }, Workbooks().Select(parse).ToArray());
    }
  }
}
