using System.Collections.Generic;

namespace Unrect.Projections
{
  /// <summary>
  /// How a declaration path renders — the one place that decides what a reader calls a definition,
  /// which wrappers a path skips, how scaffolding folds and when a quoted name earns its kind
  /// suffix. Pure over the data face: it reads <see cref="IProjectionDefinition"/> and
  /// <see cref="UseSite"/> and holds nothing, so the pull context and the push scope render one
  /// chain the same way.
  /// </summary>
  internal static class PathRenderer
  {
    internal static string Describe(IProjectionDefinition projection) => Describe(projection, default);

    /// <summary>
    /// What a reader should call this projection, best name first: the one it was given, then the
    /// one the declaration wrote at the use site, and failing both its kind and which child it is.
    /// The middle rung renders exactly like the first, because a label that named the path but not
    /// the subject would have one message calling the same child two things.
    /// </summary>
    internal static string Describe(IProjectionDefinition projection, UseSite site)
      => projection.Name is not null ? $"'{projection.Name}'"
       : site.Name is not null ? $"'{site.Name}'"
       : site.Ordinal is int ordinal ? $"{projection.Description}#{ordinal}"
       : projection.Description;

    /// <summary>
    /// The projection a reader would name. Wrappers that a path skips — an unnamed <c>Select</c>
    /// unifying variants, a boundary declaring tolerance — say nothing useful about themselves, so
    /// they stand in for what they wrap.
    /// </summary>
    internal static IProjectionDefinition Through(IProjectionDefinition projection)
    {
      while (Skipped(projection) && projection.Children.Count > 0)
        projection = projection.Children[0].Definition;

      return projection;
    }

    /// <summary>
    /// The renderer's one rule about wrappers: an unnamed wrapper carrying no unit label is not a
    /// level of the path. The projection publishes the structural fact (<see
    /// cref="IProjectionDefinition.IsWrapper"/>); whether that fact hides it is decided here and nowhere else.
    /// </summary>
    internal static bool Skipped(IProjectionDefinition projection)
      => projection.IsWrapper && projection.Name is null && projection.UnitName is null;

    internal static string DescribeThrough(IProjectionDefinition projection) => Describe(Through(projection));

    /// <summary>
    /// The whole path with no boundary folded — a debugger drill-through. A boundary renders its unit
    /// name unquoted; everything else renders as it always has, including the trailing kind suffix a
    /// named leaf earns.
    /// </summary>
    internal static string RenderFull(List<PathNode> chain)
    {
      if (chain.Count == 0)
        return "(root)";

      var segments = new List<string>(chain.Count);
      foreach (var node in chain)
        segments.Add(Segment(node));

      ApplyKindSuffix(segments, chain[chain.Count - 1]);
      return string.Join(" -> ", segments);
    }

    /// <summary>
    /// The collapsed path and its subject. A node a factory marked scaffolding contributes no
    /// segment, carrying only its occurrence index up onto the nearest segment that was kept;
    /// everything the declaration wrote keeps its own. With no scaffolding in the chain nothing
    /// folds, so this is byte-identical to <see cref="RenderFull"/>, kind suffix and all. The
    /// subject always names the deepest surviving segment, so it and the collapsed path's tail
    /// agree.
    /// </summary>
    internal static (string Path, string Subject) Collapse(List<PathNode> chain)
    {
      if (chain.Count == 0)
        return ("(root)", "(root)");

      var segments = new List<string>();
      var lastKept = 0;
      var surviving = chain[0];

      foreach (var node in chain)
      {
        if (!node.Projection.IsScaffolding)
          Keep(node);
        // With nothing kept above it there is no segment to carry the index up onto, so it is
        // dropped rather than moved down onto whatever the fold keeps next; RenderFull still has it.
        else if (node.Index is int index && segments.Count > 0)
          segments[lastKept] += $"[{index}]";
      }

      // Every node was scaffolding, so the fold left no segment to speak of or to suffix. The
      // subject still names the root, which is the one thing left that a reader can act on.
      if (segments.Count == 0)
        return ("(root)", SegmentName(surviving));

      // The suffix says what a quoted name hides, and the deepest node is what it would say — so
      // scaffolding there has no name to speak for and nothing to add.
      if (!chain[chain.Count - 1].Projection.IsScaffolding)
        ApplyKindSuffix(segments, chain[chain.Count - 1]);

      return (string.Join(" -> ", segments), SegmentName(surviving));

      void Keep(PathNode kept)
      {
        segments.Add(Segment(kept));
        lastKept = segments.Count - 1;
        surviving = kept;
      }
    }

    /// <summary>What a projection contributes to a path: its name, plus its occurrence index if it has one.</summary>
    private static string Segment(PathNode node)
      => SegmentName(node) + (node.Index is int index ? $"[{index}]" : string.Empty);

    /// <summary>
    /// A boundary's unit label, unquoted, joined to its instance name as <c>label:name</c> when it
    /// also carries one; anything else as a reader would name it.
    /// </summary>
    private static string SegmentName(PathNode node)
    {
      if (node.Projection.UnitName is not string label)
        return Describe(node.Projection, node.Site);

      return node.Projection.Name is string instance ? $"{label}:{instance}" : label;
    }

    /// <summary>
    /// A name hides what the projection is, so the last segment says so — whether the name was
    /// declared on the projection or read off the use site, since both render as a quoted name.
    /// </summary>
    private static void ApplyKindSuffix(List<string> segments, PathNode deepest)
    {
      if (deepest.Projection.Name is not null || deepest.Site.Name is not null)
        segments[segments.Count - 1] += $" ({Kind(deepest.Projection.Description)})";
    }

    private static string Kind(string description)
    {
      var parenthesis = description.IndexOf('(');
      return parenthesis < 0 ? description : description.Substring(0, parenthesis);
    }
  }

  /// <summary>
  /// One projection in a rendered path: the projection, the use site that labels it, and its
  /// occurrence index. Materialised once per failure so the collapsed path and the full path render
  /// the same chain two ways.
  /// </summary>
  internal readonly struct PathNode
  {
    public PathNode(IProjectionDefinition projection, UseSite site, int? index)
    {
      Projection = projection;
      Site = site;
      Index = index;
    }

    public IProjectionDefinition Projection { get; }

    public UseSite Site { get; }

    public int? Index { get; }
  }

}
