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
    private ShapeContext(ShapeContext? parent, IShape? shape, int? index, Offset origin)
    {
      Parent = parent;
      Shape = shape;
      Index = index;
      Origin = origin;
    }

    public static ShapeContext Root(ISpace space)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return new ShapeContext(null, null, null, default);
    }

    public ShapeContext? Parent { get; }
    public IShape? Shape { get; }
    public int? Index { get; }
    public Offset Origin { get; }

    /// <summary>
    /// The declaration path to this context, e.g.
    /// <c>Vertical -&gt; 'investor details'[2] -&gt; 'investor name' (Cell)</c>.
    /// </summary>
    public string Path => Render(Shape);

    public ShapeContext Descend(IShape shape, Offset offset, int? index = null)
      => new ShapeContext(this, shape, index, Origin + offset);

    /// <summary>
    /// Moves the origin without adding a path segment — how stacks and repeats track their cursor.
    /// </summary>
    public ShapeContext Advance(Offset offset)
      => new ShapeContext(Parent, Shape, Index, Origin + offset);

    public ShapeLocation Locate(ISpace space)
      => new ShapeLocation(Origin.Size.Height + 1, Origin.Size.Width + 1, space.Area.Size);

    public ShapeException Failure(string problem, ISpace space, Exception? inner = null)
      => Failure(
        Shape ?? throw new InvalidOperationException("The root context has no shape to blame; report failures from within a shape's projection."),
        problem,
        space,
        null,
        inner);

    internal ShapeContext WithIndex(int index) => new ShapeContext(Parent, Shape, index, Origin);

    internal ShapeException Failure(IShape shape, string problem, ISpace space, Size? requested, Exception? inner)
      => new ShapeException(Describe(shape), problem, Render(shape), Locate(space), requested, shape, inner);

    private static string Describe(IShape shape) => shape.Name is null ? shape.Description : $"'{shape.Name}'";

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
}
