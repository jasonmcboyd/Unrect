using System;
using System.Collections.Generic;
using System.Text;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a declaration costs to run as a forward pass, before any space is read: for every node,
  /// whether the engine drives it span by span or holds it whole, how far back its own machine may
  /// still read once placed, and the axis it announces. A pure function of the definition tree and the driver's
  /// orientation, so it can be printed from a test or a script with no file in hand.
  /// <para>
  /// A held node is driven again along its own axis once its extent is known, so its children are
  /// reported under that axis — a horizontal flow under a row driver holds its band, and the
  /// children inside it stream along columns. Unnamed wrappers a rendered path skips are skipped
  /// here too; a node a factory marked scaffolding is folded out unless it holds, since a hold is
  /// the very cost the report exists to show.
  /// </para>
  /// </summary>
  public sealed class CostReport
  {
    private CostReport(Orientation driver, IReadOnlyList<CostLine> lines)
    {
      Driver = driver;
      Lines = lines;
    }

    /// <summary>The orientation of the spans the root is driven along — rows for a row-major source.</summary>
    public Orientation Driver { get; }

    /// <summary>One line per reported node, in declaration order, root first.</summary>
    public IReadOnlyList<CostLine> Lines { get; }

    /// <summary>The report for <paramref name="definition"/> driven along <paramref name="driver"/> spans.</summary>
    /// <param name="definition">The declaration to cost.</param>
    /// <param name="driver">The spans the source hands the root: <see cref="Orientation.Vertical"/> for rows, the default.</param>
    public static CostReport Of(IProjectionDefinition definition, Orientation driver = Orientation.Vertical)
    {
      if (definition is null)
        throw new ArgumentNullException(nameof(definition));

      var lines = new List<CostLine>();
      Visit(definition, default, 0, driver, lines);
      return new CostReport(driver, lines);
    }

    private static void Visit(IProjectionDefinition definition, UseSite site, int depth, Orientation driver, List<CostLine> lines)
    {
      var streams = PlacementRules.Streams(definition, driver, out var hold);
      var reported = !PathRenderer.Skipped(definition) && (!definition.IsScaffolding || !streams);

      if (reported)
        lines.Add(new CostLine(depth, PathRenderer.SegmentName(definition, site), driver, streams, hold, PlacementRules.Retains(definition), definition.Axis));

      // A held node is re-driven along its own axis; one that announces none is handed its region
      // whole, and whatever it starts beneath that runs under the same driver.
      var below = streams ? driver : definition.Axis.Along(driver) ?? driver;
      var next = reported ? depth + 1 : depth;

      foreach (var child in definition.Children)
        Visit(child.Definition, child.Site, next, below, lines);
    }

    /// <summary>The lines rendered one per row, indented by depth, with the driver as a heading.</summary>
    public override string ToString()
    {
      var text = new StringBuilder();
      text.Append("driver: ").Append(Spans(Driver)).Append('\n');

      var width = 0;
      foreach (var line in Lines)
        width = Math.Max(width, line.Depth * 2 + line.Name.Length);

      foreach (var line in Lines)
      {
        var name = new string(' ', line.Depth * 2) + line.Name;
        text.Append(name.PadRight(width))
          .Append("  ")
          .Append(line.Streams ? "streams" : "holds  ")
          .Append("  retains ").Append(line.Retains.ToString().PadRight(8))
          .Append("  axis ").Append(Axis(line.Axis));

        if (line.Hold is string hold)
          text.Append("  (").Append(hold).Append(')');

        text.Append('\n');
      }

      return text.ToString();
    }

    private static string Spans(Orientation driver) => driver == Orientation.Vertical ? "rows" : "columns";

    private static string Axis(Axes axis)
      => axis == Axes.None ? "none"
       : axis == Axes.Vertical ? "vertical"
       : axis == Axes.Horizontal ? "horizontal"
       : "either";
  }

  /// <summary>One node of a <see cref="CostReport"/>.</summary>
  public readonly struct CostLine
  {
    internal CostLine(int depth, string name, Orientation driver, bool streams, string? hold, Reach retains, Axes axis)
    {
      Depth = depth;
      Name = name;
      Driver = driver;
      Streams = streams;
      Hold = hold;
      Retains = retains;
      Axis = axis;
    }

    /// <summary>How many reported nodes enclose this one.</summary>
    public int Depth { get; }

    /// <summary>What a rendered path calls the node — its unit label, its name, or its kind and position.</summary>
    public string Name { get; }

    /// <summary>The spans this node is driven along — the root's driver, or the axis of the nearest held ancestor.</summary>
    public Orientation Driver { get; }

    /// <summary>True when the engine drives the node span by span; false when it holds the node's extent and drives it afterwards.</summary>
    public bool Streams { get; }

    /// <summary>Why the node holds, when it does; null when it streams.</summary>
    public string? Hold { get; }

    /// <summary>How far back this node's own machine may still read once placed — its extent for a collector, a repeat or a tolerance boundary; a fixed number of spans for a pad or a tiler; none for a flow.</summary>
    public Reach Retains { get; }

    /// <summary>The axis the node announces it can stream along.</summary>
    public Axes Axis { get; }
  }
}
