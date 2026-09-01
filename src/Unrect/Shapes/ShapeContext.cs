using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The decomposition's position in the shape tree and on the sheet. Immutable: a fresh tree is
  /// built per <c>Map</c> call, so the same shape can be applied to many spaces at once.
  /// </summary>
  public sealed class ShapeContext
  {
    private ShapeContext(ShapeContext? parent, IShape? shape, int? index, Offset origin, DiagnosticCollector diagnostics)
    {
      Parent = parent;
      Shape = shape;
      Index = index;
      Origin = origin;
      Diagnostics = diagnostics;
    }

    public static ShapeContext Root(ISpace space)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return new ShapeContext(null, null, null, default, new DiagnosticCollector());
    }

    public ShapeContext? Parent { get; }
    public IShape? Shape { get; }
    public int? Index { get; }
    public Offset Origin { get; }

    /// <summary>
    /// Shared by reference across the whole tree of one <c>Map</c> call — the single piece of
    /// mutable state in a decomposition, and the reason contexts are per-call rather than per-shape.
    /// </summary>
    internal DiagnosticCollector Diagnostics { get; }

    /// <summary>
    /// The declaration path to this context, e.g.
    /// <c>Vertical -&gt; 'investor details'[2] -&gt; 'investor name' (Cell)</c>.
    /// </summary>
    public string Path => Render(Shape);

    public ShapeContext Descend(IShape shape, Offset offset, int? index = null)
      => new ShapeContext(this, shape, index, Origin + offset, Diagnostics);

    /// <summary>
    /// Moves the origin without adding a path segment — how stacks and repeats track their cursor.
    /// </summary>
    public ShapeContext Advance(Offset offset)
      => new ShapeContext(Parent, Shape, Index, Origin + offset, Diagnostics);

    public ShapeLocation Locate(ISpace space) => ShapeLocation.At(Origin, space.Area.Size);

    public ShapeException Failure(string problem, ISpace space, Exception? inner = null)
      => Failure(
        Shape ?? throw new InvalidOperationException("The root context has no shape to blame; report failures from within a shape's projection."),
        problem,
        space,
        null,
        inner);

    internal ShapeContext WithIndex(int index) => new ShapeContext(Parent, Shape, index, Origin, Diagnostics);

    internal ShapeException Failure(
      IShape shape,
      string problem,
      ISpace space,
      Size? requested,
      Exception? inner,
      bool isProjectionFault = false)
      => new ShapeException(Describe(shape), problem, Render(shape), Locate(space), requested, shape, inner, isProjectionFault);

    /// <summary>
    /// Records something about <paramref name="shape"/> that happened here.
    /// </summary>
    internal void Report(DiagnosticSeverity severity, IShape shape, string message, ISpace space)
    {
      var reported = Through(shape);

      Diagnostics.Add(new ShapeDiagnostic(severity, Describe(reported), message, Render(reported), Locate(space)));
    }

    /// <summary>
    /// Records something a failure caused, keeping the failure's own path and location so the
    /// diagnostic points at what went wrong rather than at whatever tolerated it. The subject and
    /// message default to the failure's own, which is what an absorbing boundary wants; a choice
    /// overrides them to speak for itself.
    /// </summary>
    internal void Report(DiagnosticSeverity severity, ShapeException failure, string? subject = null, string? message = null)
      => Diagnostics.Add(new ShapeDiagnostic(
        severity,
        subject ?? failure.Subject,
        message ?? failure.Problem,
        failure.Path,
        failure.Location));

    internal static string Describe(IShape shape) => shape.Name is null ? shape.Description : $"'{shape.Name}'";

    /// <summary>
    /// The shape a reader would name. Wrappers that a path skips — an unnamed <c>Select</c>
    /// unifying variants, a boundary declaring tolerance — say nothing useful about themselves, so
    /// they stand in for what they wrap.
    /// </summary>
    internal static IShape Through(IShape shape)
    {
      while (shape.Name is null && shape.IsTransparent && shape.Children.Count > 0)
        shape = shape.Children[0];

      return shape;
    }

    internal static string DescribeThrough(IShape shape) => Describe(Through(shape));

    /// <summary>
    /// Renders the chain of enclosing shapes, ending at <paramref name="failing"/> — which is a
    /// child of this context when a shape fails before it is descended into.
    /// </summary>
    private string Render(IShape? failing)
    {
      var segments = new List<string>();
      IShape? deepest = null;

      for (var context = this; context is not null; context = context.Parent)
      {
        if (context.Shape is not IShape shape || shape.IsTransparent)
          continue;

        segments.Insert(0, Describe(shape) + (context.Index is int index ? $"[{index}]" : string.Empty));
        deepest ??= shape;
      }

      if (failing is not null && !ReferenceEquals(deepest, failing))
      {
        segments.Add(Describe(failing));
        deepest = failing;
      }

      if (deepest is null)
        return "(root)";

      // A name hides what the shape is, so the last segment says so.
      if (deepest.Name is not null)
        segments[segments.Count - 1] += $" ({Kind(deepest.Description)})";

      return string.Join(" -> ", segments);
    }

    private static string Kind(string description)
    {
      var parenthesis = description.IndexOf('(');
      return parenthesis < 0 ? description : description.Substring(0, parenthesis);
    }
  }

  /// <summary>
  /// The diagnostics gathered during one <c>Map</c> call. A choice takes a mark before each
  /// attempt and rolls back after a failed one, so a branch that did not win leaves nothing behind.
  /// The list is created on first use: a decomposition with no boundary and no choice never
  /// allocates one.
  /// </summary>
  internal sealed class DiagnosticCollector
  {
    private List<ShapeDiagnostic>? _diagnostics;

    public int Mark() => _diagnostics?.Count ?? 0;

    public void Rollback(int mark)
    {
      if (_diagnostics is not null && _diagnostics.Count > mark)
        _diagnostics.RemoveRange(mark, _diagnostics.Count - mark);
    }

    public void Add(ShapeDiagnostic diagnostic) => (_diagnostics ??= new List<ShapeDiagnostic>()).Add(diagnostic);

    /// <summary>
    /// Whether everything recorded since <paramref name="mark"/> is one absorbed failure — a shape
    /// that failed, was tolerated, and produced nothing else to say.
    /// </summary>
    public bool AbsorbedAt(int mark)
      => _diagnostics is not null
      && _diagnostics.Count == mark + 1
      && _diagnostics[mark].Severity == DiagnosticSeverity.Warning;

    /// <summary>A copy, so what a caller reads can never change underneath it.</summary>
    public IReadOnlyList<ShapeDiagnostic> Snapshot()
      => _diagnostics is null ? System.Array.Empty<ShapeDiagnostic>() : _diagnostics.ToArray();
  }
}
