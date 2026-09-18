using System;
using System.Collections.Generic;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// The push interpreter reads exactly what the pull interpreter reads: the same value or the same
  /// failure in the same words, the same extent from the same place, the same diagnostics in the
  /// same order. Each case is one declaration over one space, observed through both engines and
  /// compared at L3. Phase 4 runs the whole suite this way; these are the cases that pin each
  /// machine as it lands.
  /// </summary>
  public class PushEquivalenceTests
  {
    public static IEnumerable<object[]> Cases()
    {
      var ladder = Ladder();
      var grid = CoordinateGrid();
      var mixed = Mixed(new object?[,]
      {
        { "Title", null, null },
        { null, null, null },
        { "Investor", "Amount", null },
        { "Acme", 10m, null },
        { "Bolt", 20m, null },
        { null, null, null },
        { "Total", 30m, null },
      });

      var cases = new PushCase[]
      {
        Case("one leaf", IntCell(), ladder),
        Case("a leaf placed down", Down(1).Of(IntCell()), ladder),
        Case("a leaf past the space", Down(5).Of(IntCell()), ladder),
        Case("text where a number is", IntCell(), Mixed(new object?[,] { { "x" } })),
        Case("nothing", NothingDefinition<ISheetCells, int>.Instance, ladder),
        Case("a vertical flow of leaves", VerticalFlow(v => { var a = v.Next(IntCell()); var b = v.Next(IntCell()); return v.Build(r => $"{r.Of(a)}|{r.Of(b)}"); }), ladder),
        Case("a flow with a select", VerticalFlow(v => { var a = v.Next(IntCell().Select(i => i * 10)); var b = v.Next(IntCell()); return v.Build(r => r.Of(a) + r.Of(b)); }), ladder),
        Case("a flow whose fourth child is missing", VerticalFlow(v => { var a = v.Next(IntCell()); var b = v.Next(IntCell()); var c = v.Next(IntCell()); var d = v.Next(IntCell()); return v.Build(r => r.Of(a) + r.Of(b) + r.Of(c) + r.Of(d)); }), ladder),
        Case("a horizontal flow", HorizontalFlow(h => { var a = h.Next(IntCell()); var b = h.Next(IntCell()); return h.Build(r => $"{r.Of(a)}|{r.Of(b)}"); }), grid),
        Case("a nested flow", VerticalFlow(v => { var a = v.Next(IntCell()); var w = v.Next(HorizontalFlow(h => { var c = h.Next(IntCell()); var d = h.Next(IntCell()); return h.Build(r => r.Of(c) + r.Of(d)); })); return v.Build(r => $"{r.Of(a)}:{r.Of(w)}"); }), grid),
        Case("an overlay", Overlay(o => { var a = o.Next(IntCell()); var b = o.Next(Down(1).Of(IntCell())); var c = o.Next(Right(1).Of(IntCell())); return o.Build(r => $"{r.Of(a)}|{r.Of(b)}|{r.Of(c)}"); }), grid),
        Case("a row strip", Row(s => s.Count), grid),
        Case("a column strip", Column(c => c.Count), grid),
        Case("a range", Range(b => $"{b.Width}x{b.Height}"), grid),
        Case("a sized range", Range(2, 2, b => $"{b.Width}x{b.Height}"), grid),
        Case("a caption", Caption("Investor"), mixed),
        Case("a missing caption", Caption("Nope"), mixed),
        Case("a padded leaf", IntCell().Padded(1), grid),
        Case("a padded flow", VerticalFlow(v => { var a = v.Next(IntCell()); var b = v.Next(IntCell()); return v.Build(r => r.Of(a) + r.Of(b)); }).Padded(0, 1), grid),
        Case("below a landmark", Below(RowContaining("Investor")).Of(Text()), mixed),
        Case("on a landmark, sized while any value", On(RowContaining("Acme")).Sized(RowsWhileAnyValue()).Of(Range(b => b.Height)), mixed),
        Case("after blank rows", AfterBlankRows().Of(Text()), Mixed(new object?[,] { { null }, { null }, { "x" } })),
        Case("a flow over a discovered block", VerticalFlow(v => { var t = v.Next(Text()); var rows = v.Next(AfterBlankRows().Sized(RowsWhileAnyValue()).Of(Range(b => b.Height))); return v.Build(r => $"{r.Of(t)}:{r.Of(rows)}"); }), mixed),
        Case("a select that throws in its lambda", IntCell().Select<ISheetCells, int, int>(_ => throw new InvalidOperationException("boom")), ladder),
      };

      foreach (var pushCase in cases)
        yield return new object[] { pushCase };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ThePushInterpreterReadsWhatThePullInterpreterReads(PushCase pushCase)
      => AssertL3(pushCase.Pull(), pushCase.Push());

    private static PushCase Case<T>(string name, IProjectionDefinition<ISheetCells, T> projection, ISheetCells space)
      => new PushCase(name, () => Observe(projection, space), () => ObservePush(projection, space));

    /// <summary>One declaration over one space, read both ways. Public so a theory can take it; the readings are the test's own.</summary>
    public sealed class PushCase
    {
      internal PushCase(string name, Func<Observation> pull, Func<Observation> push)
      {
        Name = name;
        Pull = pull;
        Push = push;
      }

      public string Name { get; }

      internal Func<Observation> Pull { get; }

      internal Func<Observation> Push { get; }

      public override string ToString() => Name;
    }
  }
}
