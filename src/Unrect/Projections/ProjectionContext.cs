using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The decomposition's position in the projection tree and on the sheet. Immutable: a fresh tree
  /// is built per <c>Map</c> call, so the same projection can be applied to many spaces at once.
  /// </summary>
  public sealed class ProjectionContext
  {
    private ProjectionContext(
      ProjectionContext? parent,
      IProjection? projection,
      int? index,
      Offset origin,
      ISpace space,
      DiagnosticCollector diagnostics,
      UseSite site,
      UseSite pending,
      LabelScope? labels,
      int? ordinal)
    {
      Parent = parent;
      Space = space;
      Projection = projection;
      Index = index;
      Origin = origin;
      Diagnostics = diagnostics;
      Site = site;
      Pending = pending;
      Labels = labels;
      Ordinal = ordinal;
    }

    /// <summary>
    /// The context a <c>Map</c> call starts from: no projection yet, no path, origin (0, 0), and a
    /// fresh diagnostic collector for this decomposition.
    /// </summary>
    public static ProjectionContext Root(ISpace space)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return new ProjectionContext(null, null, null, default, space, new DiagnosticCollector(), default, default, null, null);
    }

    private ProjectionContext? Parent { get; }

    /// <summary>
    /// The space the decomposition started from. Nothing reads it yet; it is here so the root owns
    /// what it was given rather than discarding it, which is what a decomposition trace will hang
    /// off when wave 3 adds one.
    /// </summary>
    internal ISpace Space { get; }

    /// <summary>The projection this context is inside, or null at the root.</summary>
    public IProjection? Projection { get; }

    /// <summary>Which occurrence of <see cref="Projection"/> this is, where that is meaningful (e.g. inside a repeat).</summary>
    public int? Index { get; }

    /// <summary>
    /// The occurrence number the nearest enclosing repeat is on, or null outside one. Unlike
    /// <see cref="Index"/> — which names the path segment and is cleared on <see cref="Descend"/> so
    /// a repeat's item does not inherit its parent's ordinal into its own children — this is copied
    /// unchanged through <see cref="Descend"/> and so persists into the whole subtree the repeat
    /// manufactures. That is what lets a decoupled record recover the body row it is projecting when
    /// the path index has already been consumed by the repeat's own segment. A nested repeat
    /// overwrites it, so the nearest enclosing one wins.
    /// </summary>
    internal int? Ordinal { get; }

    /// <summary>Where this context sits, relative to the space the root <c>Map</c> call was given.</summary>
    public Offset Origin { get; }

    /// <summary>
    /// Shared by reference across the whole tree of one <c>Map</c> call — the single piece of
    /// mutable state in a decomposition, and the reason contexts are per-call rather than
    /// per-projection.
    /// </summary>
    internal DiagnosticCollector Diagnostics { get; }

    /// <summary>Where this context's own projection was used — the label its segment renders with.</summary>
    private UseSite Site { get; }

    /// <summary>
    /// The use site waiting for the next <see cref="Descend"/>. A layout sets it just before
    /// handing a child to the engine; the child claims it on the way in.
    /// </summary>
    private UseSite Pending { get; }

    /// <summary>
    /// The ambient label environment: a stack of <see cref="LabelScope"/> cons-cells a labelling
    /// projection pushes for its declaration subtree, or null where none is in scope. Copied
    /// unchanged through every in-tree move, so a label reaches every descendant a labelling
    /// projection manufactures — the scope is the declaration subtree, not any region of the sheet.
    /// </summary>
    private LabelScope? Labels { get; }

    /// <summary>
    /// Pushes <paramref name="source"/> as the nearest set of labels along <paramref name="axis"/>,
    /// capturing this context's <see cref="Origin"/> as the frame the labels' ordinals are relative
    /// to. A leaf that later resolves one of these labels translates the ordinal from that captured
    /// frame to its own.
    /// </summary>
    internal ProjectionContext PushLabels(LabelAxis axis, ILabelSource source)
      => new ProjectionContext(Parent, Projection, Index, Origin, Space, Diagnostics, Site, Pending, new LabelScope(axis, source, Origin, Labels), Ordinal);

    /// <summary>
    /// The nearest labels along <paramref name="axis"/>, or null when none is in scope. Walks the
    /// stack from the inside out, so an inner scope shadows an outer one on the same axis; a scope on
    /// a different axis is skipped, so a row-labelled and a column-labelled scope coexist.
    /// </summary>
    internal LabelScope? NearestLabels(LabelAxis axis)
    {
      for (var scope = Labels; scope is not null; scope = scope.Outer)
        if (scope.Axis == axis)
          return scope;

      return null;
    }

    /// <summary>
    /// Enters <paramref name="projection"/>, which claims whatever use site was waiting for it.
    /// Nothing is left over: a projection's own children are labelled by their own use sites, not
    /// by its.
    /// </summary>
    public ProjectionContext Descend(IProjection projection, Offset offset)
      => new ProjectionContext(this, projection, null, Origin + offset, Space, Diagnostics, Pending, default, Labels, Ordinal);

    /// <summary>
    /// Moves the origin without adding a path segment — how layouts and repeats track their cursor.
    /// The same node moved, so it keeps both use sites; that is also what the engine does for a
    /// transparent projection, and therefore how a label reaches past a wrapper to the projection a
    /// reader would name.
    /// </summary>
    public ProjectionContext Advance(Offset offset)
      => new ProjectionContext(Parent, Projection, Index, Origin + offset, Space, Diagnostics, Site, Pending, Labels, Ordinal);

    /// <summary>Declares where the next child was written, for it to claim on the way in.</summary>
    internal ProjectionContext WithUseSite(UseSite site)
      => new ProjectionContext(Parent, Projection, Index, Origin, Space, Diagnostics, Site, site, Labels, Ordinal);

    /// <summary>Where this context sits, expressed as an A1-style address against <paramref name="space"/>'s extent.</summary>
    public ProjectionLocation Locate(ISpace space) => ProjectionLocation.At(Origin, space.Area.Size);

    /// <summary>
    /// A <see cref="ProjectionException"/> blaming this context's own projection, for a projection
    /// to throw when the data it was handed is not what the projection declared.
    /// </summary>
    public ProjectionException Failure(string problem, ISpace space, Exception? inner = null)
      => Failure(
        Projection ?? throw new InvalidOperationException("The root context has no projection to blame; report failures from within a projection's Project."),
        problem,
        space,
        null,
        inner);

    /// <summary>
    /// The same failure as the public <see cref="Failure(string, ISpace, Exception?)"/>, carrying
    /// the fault flag. An overload rather than an optional parameter on the public method: adding a
    /// parameter there would be a binary break, and the flag is not a caller's to set.
    /// </summary>
    internal ProjectionException Failure(string problem, ISpace space, Exception? inner, bool isFault)
      => Failure(
        Projection ?? throw new InvalidOperationException("The root context has no projection to blame; report failures from within a projection's Project."),
        problem,
        space,
        null,
        inner,
        isFault);

    internal ProjectionContext WithIndex(int index)
      => new ProjectionContext(Parent, Projection, index, Origin, Space, Diagnostics, Site, Pending, Labels, Ordinal);

    /// <summary>
    /// Stamps the occurrence number a repeat is applying, so the item's whole subtree can recover it
    /// through <see cref="Ordinal"/>. Distinct from <see cref="WithIndex"/>, which sets the
    /// path-rendering index: this survives the <see cref="Descend"/> into the item, that one does not.
    /// </summary>
    internal ProjectionContext WithOrdinal(int ordinal)
      => new ProjectionContext(Parent, Projection, Index, Origin, Space, Diagnostics, Site, Pending, Labels, ordinal);

    internal ProjectionException Failure(
      IProjection projection,
      string problem,
      ISpace space,
      Size? requested,
      Exception? inner,
      bool isFault = false)
    {
      var chain = Chain(projection);
      var (path, subject) = Collapse(chain);

      return new ProjectionException(subject, problem, path, RenderFull(chain), Locate(space), requested, projection, inner, isFault);
    }

    /// <summary>
    /// Records something about <paramref name="projection"/> that happened here.
    /// </summary>
    internal void Report(DiagnosticSeverity severity, IProjection projection, string message, ISpace space)
    {
      var chain = Chain(Through(projection));
      var (path, subject) = Collapse(chain);

      Diagnostics.Add(new ProjectionDiagnostic(severity, subject, message, path, RenderFull(chain), Locate(space)));
    }

    /// <summary>
    /// Which use site labels <paramref name="projection"/>: its own if this context is already
    /// inside it, otherwise the one waiting for it — the same discrimination the renderer makes
    /// between a context's own projection and a child being placed but not yet descended into.
    /// </summary>
    private UseSite SiteOf(IProjection projection) => ReferenceEquals(Projection, projection) ? Site : Pending;

    /// <summary>
    /// Records something a failure caused, keeping the failure's own path and location so the
    /// diagnostic points at what went wrong rather than at whatever tolerated it. The subject and
    /// message default to the failure's own, which is what an absorbing boundary wants; a choice
    /// overrides them to speak for itself.
    /// </summary>
    internal void Report(DiagnosticSeverity severity, ProjectionException failure, string? subject = null, string? message = null)
      => Diagnostics.Add(new ProjectionDiagnostic(
        severity,
        subject ?? failure.Subject,
        message ?? failure.Problem,
        failure.Path,
        failure.FullPath,
        failure.Location));

    internal static string Describe(IProjection projection) => Describe(projection, default);

    /// <summary>
    /// What a reader should call this projection, best name first: the one it was given, then the
    /// one the declaration wrote at the use site, and failing both its kind and which child it is.
    /// The middle rung renders exactly like the first, because a label that named the path but not
    /// the subject would have one message calling the same child two things.
    /// </summary>
    private static string Describe(IProjection projection, UseSite site)
      => projection.Name is not null ? $"'{projection.Name}'"
       : site.Name is not null ? $"'{site.Name}'"
       : site.Ordinal is int ordinal ? $"{projection.Description}#{ordinal}"
       : projection.Description;

    /// <summary>
    /// The projection a reader would name. Wrappers that a path skips — an unnamed <c>Select</c>
    /// unifying variants, a boundary declaring tolerance — say nothing useful about themselves, so
    /// they stand in for what they wrap.
    /// </summary>
    internal static IProjection Through(IProjection projection)
    {
      while (projection.Name is null && projection.IsTransparent && projection.Children.Count > 0)
        projection = projection.Children[0];

      return projection;
    }

    internal static string DescribeThrough(IProjection projection) => Describe(Through(projection));

    /// <summary>
    /// The chain of enclosing projections, root to leaf, ending at <paramref name="failing"/> — a
    /// child of this context when a projection fails before it is descended into. Transparent
    /// wrappers say nothing about themselves and are left out.
    /// </summary>
    private List<PathNode> Chain(IProjection? failing)
    {
      var chain = new List<PathNode>();
      IProjection? deepest = null;

      for (var context = this; context is not null; context = context.Parent)
      {
        if (context.Projection is not IProjection projection || projection.IsTransparent)
          continue;

        chain.Insert(0, new PathNode(projection, context.Site, context.Index));
        deepest ??= projection;
      }

      if (failing is not null && !ReferenceEquals(deepest, failing))
        chain.Add(new PathNode(failing, Pending, null));

      return chain;
    }

    /// <summary>
    /// The whole path with no boundary folded — a debugger drill-through. A boundary renders its unit
    /// name unquoted; everything else renders as it always has, including the trailing kind suffix a
    /// named leaf earns.
    /// </summary>
    private static string RenderFull(List<PathNode> chain)
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
    /// The collapsed path and its subject. Inside a boundary, only quoted-name survivors keep a
    /// segment of their own; every other node folds, carrying only its occurrence index up onto the
    /// nearest surviving segment. With no boundary in the chain nothing folds, so this is
    /// byte-identical to <see cref="RenderFull"/>, kind suffix and all, and the subject is the
    /// failing node's own description. The subject always names the deepest surviving segment, so it
    /// and the collapsed path's tail agree.
    /// </summary>
    private static (string Path, string Subject) Collapse(List<PathNode> chain)
    {
      if (chain.Count == 0)
        return ("(root)", "(root)");

      var segments = new List<string>();
      var insideBoundary = false;
      var lastKept = 0;
      var surviving = chain[0];

      foreach (var node in chain)
      {
        if (node.Projection.IsUnitBoundary)
        {
          insideBoundary = true;
          Keep(node);
        }
        else if (!insideBoundary || node.Projection.Name is not null || node.Site.Name is not null)
        {
          Keep(node);
        }
        else if (node.Index is int index)
        {
          segments[lastKept] += $"[{index}]";
        }
      }

      if (!insideBoundary)
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
      if (!node.Projection.IsUnitBoundary)
        return Describe(node.Projection, node.Site);

      var label = ((ProjectionBase)node.Projection).UnitName!;
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
    public PathNode(IProjection projection, UseSite site, int? index)
    {
      Projection = projection;
      Site = site;
      Index = index;
    }

    public IProjection Projection { get; }

    public UseSite Site { get; }

    public int? Index { get; }
  }

  /// <summary>
  /// Where a child was written, as far as the compiler could tell: the identifier the declaration
  /// used for it, and which child of its parent it is. The label belongs to the use site rather
  /// than to the projection, so the same projection declared in two places is called two different
  /// things.
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
    /// The lower two rungs of the naming ladder, applied wherever a declaration captures the text
    /// of an argument. A child written as a plain identifier is called that, verbatim — the point
    /// of the label is to lead a reader back to the line that produced it, so humanising it would
    /// break the grep and invent a name nobody wrote. Anything else — an inline factory call, a
    /// member access, a modifier chain — has no name to borrow and falls back to
    /// <paramref name="ordinal"/>, or to its description where there is no ordinal either. The top
    /// rung needs no code: a projection's own name always wins.
    /// </summary>
    public static UseSite From(string? declared, int? ordinal)
      => new UseSite(IsIdentifier(declared) ? declared : null, ordinal);

    /// <summary>
    /// Whether <paramref name="text"/> is a bare ASCII identifier. Hand-rolled rather than a
    /// regular expression: no dependency, no allocation, and the rule is short enough to read.
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

  /// <summary>
  /// Which axis a set of labels names: a <see cref="Column"/> label answers a column, translating
  /// along <c>Offset.Width</c>; a <see cref="Row"/> label answers a row, translating along
  /// <c>Offset.Height</c>. Kept separate on the stack so a row-labelled and a column-labelled scope
  /// coexist rather than shadow one another.
  /// </summary>
  internal enum LabelAxis
  {
    Column,
    Row,
  }

  /// <summary>
  /// Something that answers a label with the ordinals carrying it, in the frame it was captured in.
  /// A <c>TableView</c> is one (its header); step 2's public <c>LabelMap</c> will be another, so the
  /// context shape is already what a scope-introducer pushes.
  /// </summary>
  internal interface ILabelSource
  {
    /// <summary>Every label along the axis, in order — the values a resolver lists when a lookup misses.</summary>
    IReadOnlyList<string> Labels { get; }

    /// <summary>The ordinals carrying <paramref name="label"/>, in the captured frame; empty when none does.</summary>
    IReadOnlyList<int> IndicesOf(string label);
  }

  /// <summary>
  /// One entry in the ambient label environment: a set of labels along an axis, the origin they were
  /// captured at, and the scope it shadows. Immutable — a cons-cell in the context's label stack.
  /// </summary>
  internal sealed class LabelScope
  {
    public LabelScope(LabelAxis axis, ILabelSource source, Offset captureOrigin, LabelScope? outer)
    {
      Axis = axis;
      Source = source;
      CaptureOrigin = captureOrigin;
      Outer = outer;
    }

    /// <summary>Which axis these labels name.</summary>
    public LabelAxis Axis { get; }

    /// <summary>Where the labels' ordinals are read — the answer to <c>IndicesOf</c> is in this frame.</summary>
    public ILabelSource Source { get; }

    /// <summary>The <c>Origin</c> the scope was pushed at, the frame its ordinals translate from.</summary>
    public Offset CaptureOrigin { get; }

    /// <summary>The scope this one shadows, or null at the bottom of the stack.</summary>
    public LabelScope? Outer { get; }
  }
}
