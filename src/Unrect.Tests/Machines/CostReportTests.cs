using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// The dry-run cost report: what a declaration holds and what it streams, read off the tree with
  /// no space in hand, and agreeing with the engine because both ask PlacementRules.
  /// </summary>
  public class CostReportTests
  {
    private static CostLine Line(CostReport report, string name) => report.Lines.Single(line => line.Name == name);

    [Fact]
    public void AFlowOfLeavesOverATableStreamsEndToEnd()
    {
      var report = CostReport.Of(VerticalFlow(v =>
      {
        var title = v.Next(Text().Named("title"));
        var rows = v.Next(AfterBlankRows().Of(Table()));

        return v.Build(read => $"{read.Of(title)}:{read.Of(rows).Count}");
      }));

      Assert.All(report.Lines, line => Assert.True(line.Streams, $"{line.Name}: {line.Hold}"));
      Assert.Equal(Orientation.Vertical, report.Driver);
      Assert.Equal("VerticalFlow", report.Lines[0].Name);
      Assert.Equal(0, report.Lines[0].Depth);
      Assert.Equal(1, Line(report, "'title'").Depth);
      Assert.Equal(Axes.Vertical, report.Lines[0].Axis);
      Assert.Equal(new[] { "VerticalFlow", "'title'", "Table#2" }, report.Lines.Select(line => line.Name));
    }

    [Fact]
    public void ABlockStreamsUnderTheVocabularysOwnAreasAndHoldsUnderAnOpaqueOne()
    {
      // The bare Range is the discovered block, whose scan answers per row; a lambda area is a
      // function of the whole plane, its scan answers only at the end, and the block that carries
      // it is held and placed at close.
      var discovered = CostReport.Of(Range(b => b.Height));
      var opaque = CostReport.Of(Sized(SelectArea(plane => new Size(1, 1))).Of(Range(b => b.Height)));

      Assert.True(discovered.Lines[0].Streams);
      Assert.Null(discovered.Lines[0].Hold);
      Assert.Equal(Axes.None, discovered.Lines[0].Axis);

      Assert.False(opaque.Lines[0].Streams);
      Assert.Equal("its placement answers only over its whole extent under a row driver", opaque.Lines[0].Hold);
    }

    [Fact]
    public void AHorizontalFlowUnderARowDriverHoldsAndItsChildrenRunAlongColumns()
    {
      var report = CostReport.Of(HorizontalFlow(h =>
      {
        var a = h.Next(Text().Named("a"));
        var b = h.Next(Text().Named("b"));

        return h.Build(read => read.Of(a) + read.Of(b));
      }));

      var flow = report.Lines[0];
      Assert.False(flow.Streams);
      Assert.Equal("it streams along columns only", flow.Hold);
      Assert.Equal(Orientation.Vertical, flow.Driver);

      var a = Line(report, "'a'");
      Assert.Equal(Orientation.Horizontal, a.Driver);
      Assert.True(a.Streams);
      Assert.Equal(1, a.Depth);
    }

    [Fact]
    public void AColumnLandmarkHoldsBecauseItsPlacementHasNoPerSpanFormAlongRows()
    {
      var report = CostReport.Of(RightOf(ColumnContaining("Investor")).Of(Text()));

      Assert.False(report.Lines[0].Streams);
      Assert.Equal("its placement answers only over its whole extent under a row driver", report.Lines[0].Hold);
    }

    [Fact]
    public void TheSameDeclarationCostsDifferentlyUnderAColumnDriver()
    {
      var flow = VerticalFlow(v =>
      {
        var a = v.Next(Text());

        return v.Build(read => read.Of(a));
      });

      Assert.True(CostReport.Of(flow, Orientation.Vertical).Lines[0].Streams);
      Assert.False(CostReport.Of(flow, Orientation.Horizontal).Lines[0].Streams);
      Assert.Equal("it streams along rows only", CostReport.Of(flow, Orientation.Horizontal).Lines[0].Hold);
    }

    [Fact]
    public void RetainsIsWhatANodesOwnMachineMayStillRead()
    {
      var report = CostReport.Of(VerticalFlow(v =>
      {
        var one = v.Next(Text().Named("one"));
        var many = v.Next(VerticalRepeat(Text(), separatedBy: BlankRows()).Optional().Named("many"));

        return v.Build(read => $"{read.Of(one)}:{read.Of(many)?.Count}");
      }));

      Assert.Equal(Reach.None, report.Lines[0].Retains);
      Assert.Equal(Reach.Extent, Line(report, "'one'").Retains);
      Assert.Equal(Reach.Extent, Line(report, "'many'").Retains);
      Assert.All(report.Lines, line => Assert.True(line.Streams, $"{line.Name}: {line.Hold}"));
    }

    [Theory]
    [InlineData("Table(row lambda)")]
    [InlineData("Table()")]
    [InlineData("Table(view lambda)")]
    [InlineData("Table(0, eachRow)")]
    [InlineData("Table(1, eachRow)")]
    public void EveryTableRungStreamsUnderARowDriver_SizedOrPlacedByItsOwnDefault(string rung)
    {
      // A table's own extent is a discovered block whose scan answers a row at a time, so every rung
      // streams as written; .Sized changes only how the width is arrived at.
      IProjectionDefinition declared = rung switch
      {
        "Table(row lambda)" => Table(row => row.Index),
        "Table()" => Table(),
        "Table(view lambda)" => Table(table => table.RowCount),
        "Table(0, eachRow)" => Table(0, Text()),
        "Table(1, eachRow)" => Table(1, Text()),

        _ => throw new System.ArgumentOutOfRangeException(nameof(rung), rung, "No such rung."),
      };

      Assert.True(CostReport.Of(declared).Lines[0].Streams);
      Assert.True(declared.Placement.Area!.Begin(Orientation.Vertical).Incremental);
    }

    [Fact]
    public void AReflectedTableIsOneLineNamedAsItsPathSegmentIs()
    {
      var report = CostReport.Of(Table<Row>());

      // The unit is the reader's; the scaffolding inside it streams and says nothing.
      Assert.Equal("Table<Row>", report.Lines[0].Name);
      Assert.True(report.Lines[0].Streams);
      Assert.All(report.Lines, line => Assert.True(line.Streams, $"{line.Name}: {line.Hold}"));
      Assert.DoesNotContain(report.Lines, line => line.Name.Contains("VerticalBands", System.StringComparison.Ordinal));
    }

    [Fact]
    public void TheTextFormNamesTheDriverAndEveryHold()
    {
      var text = CostReport.Of(VerticalFlow(v =>
      {
        var title = v.Next(Text().Named("title"));
        var wide = v.Next(HorizontalFlow(h =>
        {
          var a = h.Next(Text());
          return h.Build(read => read.Of(a));
        }).Named("wide"));

        return v.Build(read => $"{read.Of(title)}:{read.Of(wide)}");
      })).ToString();

      Assert.StartsWith("driver: rows\n", text, System.StringComparison.Ordinal);
      Assert.Contains("VerticalFlow", text);
      Assert.Contains("'title'", text);
      Assert.Contains("streams", text);
      Assert.Contains("holds", text);
      Assert.Contains("(it streams along columns only)", text);
      Assert.Contains("axis horizontal", text);
    }

    private sealed record Row(string Name, decimal Amount);
  }
}
