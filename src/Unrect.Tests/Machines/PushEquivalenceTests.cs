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
      var table = Mixed(new object?[,] { { "Investor", "Amount" }, { "Acme", 10m }, { "Bolt", 20m } });
      var body = Mixed(new object?[,] { { "Acme", 10m }, { "Bolt", 20m } });
      var gappy = Mixed(new object?[,] { { "Investor", "Amount" }, { "Acme", 10m }, { null, null }, { "Bolt", 20m } });
      var gappyBody = Mixed(new object?[,] { { "Acme", 10m }, { null, null }, { "Bolt", 20m } });
      var totalled = Mixed(new object?[,] { { "Acme", 10m }, { "Bolt", 20m }, { "Total", 30m } });
      var blocks = Mixed(new object?[,] { { 1 }, { 2 }, { null }, { 3 }, { null }, { null }, { 4 }, { 5 } });
      var sections = Mixed(new object?[,] { { "x", null }, { "Section", 1 }, { "a", 2 }, { null, null }, { "Section", 3 }, { "b", 4 }, { "tail", 9 } });

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
        Case("a failure inside a select keeps the leaf's path", IntCell().Select(i => i + 1), Mixed(new object?[,] { { "x" } })),

        // Tables, every rung.
        Case("a composed table", Table(1, Record((TableRow<ISheetCells> r) => r["Amount"].Decimal())), table),
        Case("a headerless composed table", Table(0, Record((TableRow<ISheetCells> r) => r[1].Decimal())), body),
        Case("the dictionary rung", Table(), table),
        Case("the row lambda rung", Table((TableRow<ISheetCells> r) => r.Index), table),
        Case("the view lambda rung", Table((TableView<ISheetCells> v) => v.RowCount), table),
        Case("the bind rung", Table(1, (LabelMap labels) => Record((TableRow<ISheetCells> r) => r[labels["Amount"]].Decimal())), table),
        Case("a table over an empty extent", Table(1, Record((TableRow<ISheetCells> r) => r.Index)), Mixed(new object?[,] { { null } })),
        Case("column labels then a body", WithColumnLabels(LabelMap.Of(("Investor", 0), ("Amount", 1)), VerticalRepeat(Record((TableRow<ISheetCells> r) => r["Amount"].Decimal()))), body),
        Case("a table with a blank row skipped", Table(1, (LabelMap _) => Record((TableRow<ISheetCells> r) => r.Index), onBlank: BlankRowStrategy.Skip), gappy),
        Case("a table with a blank row tolerated", Table(1, (LabelMap _) => Record((TableRow<ISheetCells> r) => r.Index), onBlank: BlankRowStrategy.Tolerate), gappy),
        Case("a table with a blank row faulting", Table(1, (LabelMap _) => Record((TableRow<ISheetCells> r) => r.Index), onBlank: BlankRowStrategy.Fault), gappy),

        // Repetition and tiling.
        Case("a repeat of leaves", VerticalRepeat(IntCell()), ladder),
        Case("a repeat with a blank separator", VerticalRepeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows()), blocks),
        Case("a repeat that needs more than there are", VerticalRepeat(IntCell(), atLeast: 5), ladder),
        Case("a repeat ended by tolerance", VerticalRepeat(IntCell().Optional()), Mixed(new object?[,] { { 1 }, { "x" }, { 3 } })),
        Case("a horizontal repeat", HorizontalRepeat(IntCell()), grid),
        Case("a repeat then a caption", VerticalFlow(v => { var rows = v.Next(VerticalRepeat(Record((TableRow<ISheetCells> r) => r[1].Decimal()))); var total = v.Next(Caption("Total")); return v.Build(r => $"{r.Of(rows).Count}:{r.Of(total)}"); }), totalled),
        Case("a repeat of anchored items", VerticalRepeat(On(RowContaining("Section")).Of(Range(RowsWhileAnyValue(), b => b.Height))), sections),
        Case("bands of one", VerticalBands(1, IntCell()), ladder),
        Case("bands with a partial band", VerticalBands(2, Range(b => b.Height)), CoordinateGrid(height: 5)),
        Case("bands stopping at a blank row", VerticalBands(1, Record((TableRow<ISheetCells> r) => r.Index), onBlank: BlankRowStrategy.Stop), gappyBody),
        Case("bands skipping a blank row", VerticalBands(1, Record((TableRow<ISheetCells> r) => r.Index), onBlank: BlankRowStrategy.Skip), gappyBody),
        Case("horizontal bands", HorizontalBands(1, IntCell()), grid),

        // Alternation.
        Case("a choice whose first alternative wins", Choice(IntCell().Select(i => i.ToString()), Text()), ladder),
        Case("a choice whose second alternative wins", Choice(IntCell().Select(i => i.ToString()), Text()), Mixed(new object?[,] { { "x" } })),
        Case("a choice where none matches", Choice(IntCell().Select(i => i.ToString()), Date().Select(d => d.ToString())), Mixed(new object?[,] { { "x" } })),
        Case("an else with a fallback projection", IntCell().Else(Text().Select(_ => -1)), Mixed(new object?[,] { { "x" } })),
        Case("an else whose fallback fails too", IntCell().Else(Date().Select(_ => -1)), Mixed(new object?[,] { { "x" } })),
        Case("an else value", IntCell().Else(-1), Mixed(new object?[,] { { "x" } })),
        Case("an optional that reads", IntCell().Optional(), ladder),
        Case("a fault is not absorbed", IntCell().Select<ISheetCells, int, int>(_ => throw new NullReferenceException("bug")).Optional(), ladder),

        // Bounds, fields, headings.
        Case("until a landmark", Until(RowContaining("Total")).Of(Range(b => b.Height)), totalled),
        Case("until a missing landmark", Until(RowContaining("Nope")).Of(Range(b => b.Height)), totalled),
        Case("until a column", UntilColumn(ColumnContaining("Amount")).Of(Range(b => b.Width)), table),
        Case("a repeat until a landmark", Until(RowContaining("Total")).Of(VerticalRepeat(Record((TableRow<ISheetCells> r) => r[1].Decimal()))), totalled),
        Case("fields", Fields(Field("EIN"), Field("Type")), Mixed(new object?[,] { { "EIN", "12-3" }, { "Type", "LLC" } })),
        Case("a heading over a section", Heading("Investor").Of(Range(RowsWhileAnyValue(), b => b.Height)), table),
        Case("an overlay with a column-anchored child", Overlay(o => { var a = o.Next(RightOf(ColumnContaining("Investor")).Of(Text())); var b = o.Next(Text()); return o.Build(r => $"{r.Of(a)}|{r.Of(b)}"); }), table),
      };

      foreach (var pushCase in cases)
        yield return new object[] { pushCase };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ThePushInterpreterReadsWhatThePullInterpreterReads(PushCase pushCase)
    {
      var pull = pushCase.Pull();
      var push = pushCase.Push();

      try
      {
        AssertL3(pull, push);
      }
      catch (Xunit.Sdk.XunitException mismatch)
      {
        throw new Xunit.Sdk.XunitException(
          $"{mismatch.Message}\n  push value: {push.Value}\n  push consumed: {push.Consumed}, offset: {push.Offset}, advance: {push.Advance}"
          + $"\n  push failure: {push.Failure}\n  push diagnostics: {string.Join(" | ", push.Diagnostics)}"
          + $"\n  pull failure: {pull.Failure}\n  pull diagnostics: {string.Join(" | ", pull.Diagnostics)}");
      }
    }

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
