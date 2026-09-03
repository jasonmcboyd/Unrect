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
    private ShapeContext(
      ShapeContext? parent,
      IShape? shape,
      int? index,
      Offset origin,
      ISpace space,
      DiagnosticCollector diagnostics,
      UseSite site,
      UseSite pending)
    {
      Parent = parent;
      Space = space;
      Shape = shape;
      Index = index;
      Origin = origin;
      Diagnostics = diagnostics;
      Site = site;
      Pending = pending;
    }

    /// <summary>
    /// The context a <c>Map</c> call starts from: no shape yet, no path, origin (0, 0), and a fresh
    /// diagnostic collector for this decomposition.
    /// </summary>
    public static ShapeContext Root(ISpace space)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return new ShapeContext(null, null, null, default, space, new DiagnosticCollector(), default, default);
    }

    private ShapeContext? Parent { get; }

    /// <summary>
    /// The space the decomposition started from. Nothing reads it yet; it is here so the root owns
    /// what it was given rather than discarding it, which is what a decomposition trace will hang
    /// off when wave 3 adds one.
    /// </summary>
    internal ISpace Space { get; }

    /// <summary>The shape this context is inside, or null at the root.</summary>
    public IShape? Shape { get; }

    /// <summary>Which occurrence of <see cref="Shape"/> this is, where that is meaningful (e.g. inside a <c>Repeat</c>).</summary>
    public int? Index { get; }

    /// <summary>Where this context sits, relative to the space the root <c>Map</c> call was given.</summary>
    public Offset Origin { get; }

    /// <summary>
    /// Shared by reference across the whole tree of one <c>Map</c> call — the single piece of
    /// mutable state in a decomposition, and the reason contexts are per-call rather than per-shape.
    /// </summary>
    internal DiagnosticCollector Diagnostics { get; }

    /// <summary>Where this context's own shape was used — the label its segment renders with.</summary>
    private UseSite Site { get; }

    /// <summary>
    /// The use site waiting for the next <see cref="Descend"/>. A layout sets it just before handing
    /// a child to the engine; the child claims it on the way in.
    /// </summary>
    private UseSite Pending { get; }

    /// <summary>
    /// Enters <paramref name="shape"/>, which claims whatever use site was waiting for it. Nothing
    /// is left over: a shape's own children are labelled by their own use sites, not by its.
    /// </summary>
    public ShapeContext Descend(IShape shape, Offset offset)
      => new ShapeContext(this, shape, null, Origin + offset, Space, Diagnostics, Pending, default);

    /// <summary>
    /// Moves the origin without adding a path segment — how layouts and repeats track their cursor.
    /// The same node moved, so it keeps both use sites; that is also what the engine does for a
    /// transparent shape, and therefore how a label reaches past a wrapper to the shape a reader
    /// would name.
    /// </summary>
    public ShapeContext Advance(Offset offset)
      => new ShapeContext(Parent, Shape, Index, Origin + offset, Space, Diagnostics, Site, Pending);

    /// <summary>Declares where the next child was written, for it to claim on the way in.</summary>
    internal ShapeContext WithUseSite(UseSite site)
      => new ShapeContext(Parent, Shape, Index, Origin, Space, Diagnostics, Site, site);

    /// <summary>Where this context sits, expressed as an A1-style address against <paramref name="space"/>'s extent.</summary>
    public ShapeLocation Locate(ISpace space) => ShapeLocation.At(Origin, space.Area.Size);

    /// <summary>
    /// A <see cref="ShapeException"/> blaming this context's own shape, for a projection to throw
    /// when the data it was handed is not what the shape declared.
    /// </summary>
    public ShapeException Failure(string problem, ISpace space, Exception? inner = null)
      => Failure(
        Shape ?? throw new InvalidOperationException("The root context has no shape to blame; report failures from within a shape's projection."),
        problem,
        space,
        null,
        inner);

    /// <summary>
    /// The same failure as the public <see cref="Failure(string, ISpace, Exception?)"/>, carrying
    /// the fault flag. An overload rather than an optional parameter on the public method: adding a
    /// parameter there would be a binary break, and the flag is not a caller's to set.
    /// </summary>
    internal ShapeException Failure(string problem, ISpace space, Exception? inner, bool isFault)
      => Failure(
        Shape ?? throw new InvalidOperationException("The root context has no shape to blame; report failures from within a shape's projection."),
        problem,
        space,
        null,
        inner,
        isFault);

    internal ShapeContext WithIndex(int index)
      => new ShapeContext(Parent, Shape, index, Origin, Space, Diagnostics, Site, Pending);

    internal ShapeException Failure(
      IShape shape,
      string problem,
      ISpace space,
      Size? requested,
      Exception? inner,
      bool isFault = false)
      => new ShapeException(Describe(shape, SiteOf(shape)), problem, Render(shape), Locate(space), requested, shape, inner, isFault);

    /// <summary>
    /// Records something about <paramref name="shape"/> that happened here.
    /// </summary>
    internal void Report(DiagnosticSeverity severity, IShape shape, string message, ISpace space)
    {
      var reported = Through(shape);

      Diagnostics.Add(new ShapeDiagnostic(severity, Describe(reported, SiteOf(shape)), message, Render(reported), Locate(space)));
    }

    /// <summary>
    /// Which use site labels <paramref name="shape"/>: its own if this context is already inside it,
    /// otherwise the one waiting for it — the same discrimination the renderer makes between a
    /// context's own shape and a child being placed but not yet descended into.
    /// </summary>
    private UseSite SiteOf(IShape shape) => ReferenceEquals(Shape, shape) ? Site : Pending;

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

    internal static string Describe(IShape shape) => Describe(shape, default);

    /// <summary>
    /// What a reader should call this shape, best name first: the one it was given, then the one the
    /// declaration wrote at the use site, and failing both its kind and which child it is. The middle
    /// rung renders exactly like the first, because a label that named the path but not the subject
    /// would have one message calling the same child two things.
    /// </summary>
    private static string Describe(IShape shape, UseSite site)
      => shape.Name is not null ? $"'{shape.Name}'"
       : site.Name is not null ? $"'{site.Name}'"
       : site.Ordinal is int ordinal ? $"{shape.Description}#{ordinal}"
       : shape.Description;

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
      var deepestSite = default(UseSite);

      for (var context = this; context is not null; context = context.Parent)
      {
        if (context.Shape is not IShape shape || shape.IsTransparent)
          continue;

        segments.Insert(0, Describe(shape, context.Site) + (context.Index is int index ? $"[{index}]" : string.Empty));

        if (deepest is null)
        {
          deepest = shape;
          deepestSite = context.Site;
        }
      }

      if (failing is not null && !ReferenceEquals(deepest, failing))
      {
        segments.Add(Describe(failing, Pending));
        deepest = failing;
        deepestSite = Pending;
      }

      if (deepest is null)
        return "(root)";

      // A name hides what the shape is, so the last segment says so — whether the name was declared
      // on the shape or read off the use site, since both render as a quoted name.
      if (deepest.Name is not null || deepestSite.Name is not null)
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
  /// Where a child was written, as far as the compiler could tell: the identifier the declaration
  /// used for it, and which child of its parent it is. The label belongs to the use site rather than
  /// to the shape, so the same shape declared in two places is called two different things.
  /// </summary>
  internal readonly struct UseSite
  {
    private UseSite(string? name, int? ordinal)
    {
      Name = name;
      Ordinal = ordinal;
    }

    /// <summary>The identifier the child was written as, when it was written as a bare one.</summary>
    public string? Name { get; }

    /// <summary>
    /// Which child of its parent this is, counting from one, where that is a meaningful thing to
    /// say. A repeat has one item rather than an nth, so it supplies none.
    /// </summary>
    public int? Ordinal { get; }

    /// <summary>
    /// The lower two rungs of the naming ladder, applied wherever a declaration captures the text of
    /// an argument. A child written as a plain identifier is called that, verbatim — the point of
    /// the label is to lead a reader back to the line that produced it, so humanising it would break
    /// the grep and invent a name nobody wrote. Anything else — an inline factory call, a member
    /// access, a modifier chain — has no name to borrow and falls back to
    /// <paramref name="ordinal"/>, or to its description where there is no ordinal either. The top
    /// rung needs no code: a shape's own name always wins.
    /// </summary>
    public static UseSite From(string? declared, int? ordinal)
      => new UseSite(IsIdentifier(declared) ? declared : null, ordinal);

    /// <summary>
    /// Whether <paramref name="text"/> is a bare ASCII identifier. Hand-rolled rather than a regular
    /// expression: no dependency, no allocation, and the rule is short enough to read.
    /// </summary>
    private static bool IsIdentifier(string? text)
    {
      if (string.IsNullOrEmpty(text))
        return false;

      if (!IsLetterOrUnderscore(text![0]))
        return false;

      for (var index = 1; index < text.Length; index++)
        if (!IsLetterOrUnderscore(text[index]) && (text[index] < '0' || text[index] > '9'))
          return false;

      return true;
    }

    private static bool IsLetterOrUnderscore(char character)
      => (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || character == '_';
  }
}
