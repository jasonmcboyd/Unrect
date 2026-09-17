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
      IProjectionDefinition? projection,
      int? index,
      ISpace space,
      DiagnosticCollector diagnostics,
      UseSite site,
      UseSite pending,
      LabelScope? labels,
      int? ordinal,
      IProjectionDefinition? blame = null)
    {
      Parent = parent;
      Space = space;
      Projection = projection;
      Index = index;
      Diagnostics = diagnostics;
      Site = site;
      Pending = pending;
      Labels = labels;
      Ordinal = ordinal;
      Blame = blame;
    }

    /// <summary>
    /// The context a <c>Map</c> call starts from: no projection yet, no path, and a fresh
    /// diagnostic collector for this decomposition.
    /// </summary>
    /// <param name="space">The space the decomposition was handed.</param>
    public static ProjectionContext Root(ISpace space)
    {
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return new ProjectionContext(null, null, null, space, new DiagnosticCollector(), default, default, null, null);
    }

    private ProjectionContext? Parent { get; }

    /// <summary>
    /// The space the decomposition started from — what the root was handed, kept so a decomposition
    /// trace has something to hang off.
    /// </summary>
    internal ISpace Space { get; }

    /// <summary>The projection this context is inside, or null at the root.</summary>
    public IProjectionDefinition? Projection { get; }

    /// <summary>
    /// Who to blame at the root when nothing was entered — see <see cref="Blaming"/>. Null
    /// everywhere else, because everywhere else there is a <see cref="Projection"/>.
    /// </summary>
    private IProjectionDefinition? Blame { get; }

    /// <summary>Which occurrence of the projection this is, where that is meaningful (e.g. inside a repeat).</summary>
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
    /// with <paramref name="captureOrigin"/> — the origin of the region the labels are APPLIED to —
    /// as the frame their ordinals are relative to. A leaf that later resolves one of these labels
    /// translates the ordinal from that frame to its own region's. It is the region the labels
    /// describe rather than the one they were read from: a table pushes its own extent, and
    /// <c>WithColumnLabels</c> pushes the body it hands them to, which is what makes an ordinal
    /// mean the same column to every row beneath it.
    /// </summary>
    internal ProjectionContext PushLabels(LabelAxis axis, ILabelSource source, Offset captureOrigin)
      => new ProjectionContext(Parent, Projection, Index, Space, Diagnostics, Site, Pending, new LabelScope(axis, source, captureOrigin, Labels), Ordinal);

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
    public ProjectionContext Descend(IProjectionDefinition projection)
      => new ProjectionContext(this, projection, null, Space, Diagnostics, Pending, default, Labels, Ordinal);

    /// <summary>
    /// The root, told which projection is about to be applied to it — the one it may blame for a
    /// failure raised before anything has been entered.
    /// <para>
    /// A transparent projection contributes no path segment, so the engine does not descend into it
    /// and hands it the context it was called with. At the root that context has no projection at
    /// all, and a read failure inside the transparent projection's own lambda would have nothing to
    /// report against — which is what this closes.
    /// </para>
    /// <para>
    /// The projection itself is blamed, not what it wraps: it is what the declaration applied here,
    /// it is what the use site named, and the renderer resolves a name through a wrapper where it
    /// wants one. Blaming the wrapped projection instead would make a fallback the declaration
    /// called <c>'recovery'</c> render as <c>'recovery' (Point)</c> — the label from one projection
    /// and the kind from another.
    /// </para>
    /// <para>
    /// Anywhere but the root this is the identity: a context inside a projection already has one to
    /// blame, and blaming the transparent child instead would give it the path segment transparency
    /// exists to deny it.
    /// </para>
    /// </summary>
    internal ProjectionContext Blaming(IProjectionDefinition projection)
      => Projection is null
        ? new ProjectionContext(Parent, null, Index, Space, Diagnostics, Site, Pending, Labels, Ordinal, projection)
        : this;

    /// <summary>Declares where the next child was written, for it to claim on the way in.</summary>
    internal ProjectionContext WithUseSite(UseSite site)
      => new ProjectionContext(Parent, Projection, Index, Space, Diagnostics, Site, site, Labels, Ordinal);

    /// <summary>
    /// Where <paramref name="extent"/> starts, as an A1-style address. The region says so itself —
    /// its origin is the sheet's own — so nothing has to be accumulated on the way down to it.
    /// </summary>
    public ProjectionLocation Locate<TSpace>(Plane<TSpace> extent)
      where TSpace : class, ISpace
      => ProjectionLocation.At(extent);

    /// <summary>
    /// A <see cref="ProjectionException"/> blaming this context's own projection, for a projection
    /// to throw when the data it was handed is not what the projection declared.
    /// </summary>
    public ProjectionException Failure<TSpace>(string problem, Plane<TSpace> extent, Exception? inner = null)
      where TSpace : class, ISpace
      => Failure(Blamed(), problem, extent, null, inner);

    /// <summary>
    /// The same failure as the public <see cref="Failure{TSpace}(string, Plane{TSpace}, Exception?)"/>, carrying
    /// the fault flag. An overload rather than an optional parameter on the public method: adding a
    /// parameter there would be a binary break, and the flag is not a caller's to set.
    /// </summary>
    internal ProjectionException Failure<TSpace>(string problem, Plane<TSpace> extent, Exception? inner, bool isFault)
      where TSpace : class, ISpace
      => Failure(Blamed(), problem, extent, null, inner, isFault);

    /// <summary>
    /// The projection a failure reported from here is about: the one this context is inside, or —
    /// at the root, where nothing was entered — the one the engine said it was applying.
    /// </summary>
    private IProjectionDefinition Blamed()
      => Projection
        ?? Blame
        ?? throw new InvalidOperationException("The root context has no projection to blame; report failures from within a projection's Project.");

    /// <summary>
    /// A failed cell read, rethrown as this context's own failure: the reader's sentence, addressed
    /// the way this layer addresses a cell, carrying the declaration path and the original as its
    /// inner exception.
    /// <para>
    /// One place decides this wording, and every lambda a projection calls funnels through it — so
    /// a cell that would not read describes itself identically whether it was reached from a leaf, a
    /// row, a block or a record.
    /// </para>
    /// </summary>
    internal ProjectionException Reading<TSpace>(CellReadException failure, Plane<TSpace> extent)
      where TSpace : class, ISpace
      => Failure(failure.Problem(ProjectionLocation.At(failure.At).A1), extent, failure);

    internal ProjectionContext WithIndex(int index)
      => new ProjectionContext(Parent, Projection, index, Space, Diagnostics, Site, Pending, Labels, Ordinal);

    /// <summary>
    /// Stamps the occurrence number a repeat is applying, so the item's whole subtree can recover it
    /// through <see cref="Ordinal"/>. Distinct from <see cref="WithIndex"/>, which sets the
    /// path-rendering index: this survives the <see cref="Descend"/> into the item, that one does not.
    /// </summary>
    internal ProjectionContext WithOrdinal(int ordinal)
      => new ProjectionContext(Parent, Projection, Index, Space, Diagnostics, Site, Pending, Labels, ordinal);

    internal ProjectionException Failure<TSpace>(
      IProjectionDefinition projection,
      string problem,
      Plane<TSpace> extent,
      Size? requested,
      Exception? inner,
      bool isFault = false)
      where TSpace : class, ISpace
    {
      var chain = Chain(projection);
      var (path, subject) = Collapse(chain);

      return new ProjectionException(subject, problem, path, RenderFull(chain), Locate(extent), requested, projection, inner, isFault);
    }

    /// <summary>
    /// Records something about <paramref name="projection"/> that happened here.
    /// </summary>
    internal void Report<TSpace>(DiagnosticSeverity severity, IProjectionDefinition projection, string message, Plane<TSpace> extent)
      where TSpace : class, ISpace
    {
      var chain = Chain(Through(projection));
      var (path, subject) = Collapse(chain);

      Diagnostics.Add(new ProjectionDiagnostic(severity, subject, message, path, RenderFull(chain), Locate(extent)));
    }

    /// <summary>
    /// Which use site labels <paramref name="projection"/>: its own if this context is already
    /// inside it, otherwise the one waiting for it — the same discrimination the renderer makes
    /// between a context's own projection and a child being placed but not yet descended into.
    /// </summary>
    private UseSite SiteOf(IProjectionDefinition projection) => ReferenceEquals(Projection, projection) ? Site : Pending;

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

    internal static string Describe(IProjectionDefinition projection) => Describe(projection, default);

    /// <summary>
    /// What a reader should call this projection, best name first: the one it was given, then the
    /// one the declaration wrote at the use site, and failing both its kind and which child it is.
    /// The middle rung renders exactly like the first, because a label that named the path but not
    /// the subject would have one message calling the same child two things.
    /// </summary>
    private static string Describe(IProjectionDefinition projection, UseSite site)
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
    /// The chain of enclosing projections, root to leaf, ending at <paramref name="failing"/> — a
    /// child of this context when a projection fails before it is descended into. Wrappers the path
    /// skips say nothing about themselves and are left out.
    /// </summary>
    private List<PathNode> Chain(IProjectionDefinition? failing)
    {
      var chain = new List<PathNode>();
      IProjectionDefinition? deepest = null;

      for (var context = this; context is not null; context = context.Parent)
      {
        if (context.Projection is not IProjectionDefinition projection || Skipped(projection))
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
    /// The collapsed path and its subject. A node a factory marked scaffolding contributes no
    /// segment, carrying only its occurrence index up onto the nearest segment that was kept;
    /// everything the declaration wrote keeps its own. With no scaffolding in the chain nothing
    /// folds, so this is byte-identical to <see cref="RenderFull"/>, kind suffix and all. The
    /// subject always names the deepest surviving segment, so it and the collapsed path's tail
    /// agree.
    /// </summary>
    private static (string Path, string Subject) Collapse(List<PathNode> chain)
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
  /// The public <see cref="LabelMap"/> is one, whether it was read from a table's header or written
  /// out as literals — which is what lets a scope-introducer push either.
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

    /// <summary>
    /// The origin of the region the labels were read in, in the root space's own coordinates — the
    /// frame their ordinals translate from.
    /// </summary>
    public Offset CaptureOrigin { get; }

    /// <summary>The scope this one shadows, or null at the bottom of the stack.</summary>
    public LabelScope? Outer { get; }
  }
}
