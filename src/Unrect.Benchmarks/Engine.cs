using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Shapes;

using static Unrect.Shapes.Shape;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The composites themselves: what a child costs to place, whatever it reads. Every shape here
  /// projects as little as possible -- a cell count, a row index -- so the row measures the engine's
  /// own work (cursor advance, context descent, placement resolution, extent bookkeeping) rather
  /// than the projection hanging off it.
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Engine")]
  public class Engine
  {
    private const int FlowChildren = 5_000;
    private const int NestedRows = 500;

    // Declared once, at field initialization: a shape is a value, and building it is the Tables
    // family's subject, not this one's.
    private static readonly IShape<int> Line = Row(r => r.Count);

    private static readonly IShape<int> ManyChildren = VerticalFlow(v =>
    {
      var total = 0;

      // The N children are a loop, not N call sites: the layout lambda runs per map, so this is
      // still a declaration -- it says "N of these, stacked" without naming a row number anywhere.
      for (int i = 0; i < FlowChildren; i++)
        total += v.Next(Line);

      return total;
    });

    private static readonly IShape<int> Nested = VerticalFlow(v =>
    {
      var total = 0;

      for (int i = 0; i < NestedRows; i++)
        total += v.Next(HorizontalFlow(h => h.Next(Cell(c => c.HasValue ? 1 : 0)) + h.Next(Cell(c => 1))));

      return total;
    });

    // Four independent readings of the same band. An overlay's children each start from the band's
    // own origin, so this measures placement without the flow's advance.
    private static readonly IShape<int> Anchored = Overlay(o =>
      o.Next(Row(r => r.Count).After(To(RowContaining(CanonicalSpaces.Landmark))))
      + o.Next(Column(CanonicalSpaces.BlockRows, c => c.Count))
      + o.Next(Range(2, 2, b => b.Width))
      + o.Next(Cell(c => c.HasValue ? 1 : 0)));

    private static readonly IShape<IReadOnlyList<int>> Blocks =
      Repeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows());

    private static readonly IShape<int> AllCells = Range(b =>
    {
      var present = 0;

      for (int row = 0; row < b.Height; row++)
        for (int column = 0; column < b.Width; column++)
          if (b[column, row].HasValue)
            present++;

      return present;
    });

    private static readonly IShape<int> Section =
      Range(RowsWhileAnyValue(), b => b.Height).Under(Caption(CanonicalSpaces.DetailsCaption));

    private ISpace _tall = default!;
    private ISpace _blocks = default!;
    private ISpace _band = default!;
    private ISpace _document = default!;
    private ISpace _mixed = default!;

    [GlobalSetup]
    public void Setup()
    {
      // Fixtures are built here, never inside a measured operation.
      _tall = CanonicalSpaces.MegaDenseNumeric;
      _blocks = CanonicalSpaces.RepeatBlocks;
      _band = CanonicalSpaces.LandmarkNear;
      _document = CanonicalSpaces.LargeDocument;
      _mixed = CanonicalSpaces.MegaDenseMixed;
    }

    /// <summary>N stacked children, each consuming one row: the per-child cost of a flow.</summary>
    [Benchmark]
    public int VerticalFlow_ManyChildren() => ManyChildren.Map(_tall);

    /// <summary>A flow of flows: the same child count, one level of descent deeper.</summary>
    [Benchmark]
    public int Flow_Nested() => Nested.Map(_tall);

    /// <summary>Four children over one band, one of them content-anchored.</summary>
    [Benchmark]
    public int Overlay_AnchoredChildren() => Anchored.Map(_band);

    /// <summary>Two thousand separator-divided blocks: the repeat's per-item cost.</summary>
    [Benchmark]
    public int Repeat_SeparatedBlocks() => Blocks.Map(_blocks).Count;

    /// <summary>A section that finds itself by the caption announcing it.</summary>
    [Benchmark]
    public int Under_CaptionedSection() => Section.Map(_document);

    /// <summary>
    /// Every cell of a million-cell block, read through the view a projection actually gets. The
    /// other rows here place children and read almost nothing; this one is the opposite, and it is
    /// the only measurement of <see cref="CellBlock"/>'s indexer -- the code path between a user's
    /// lambda and the grid. Kinds are mixed, because a real projection does not read a column of
    /// identical numbers.
    /// </summary>
    [Benchmark]
    public int Range_ReadAllCells() => AllCells.Map(_mixed);
  }
}
