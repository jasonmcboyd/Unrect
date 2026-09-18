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
      var (path, subject) = PathRenderer.Collapse(chain);

      return new ProjectionException(subject, problem, path, PathRenderer.RenderFull(chain), Locate(extent), requested, projection, inner, isFault);
    }

    /// <summary>
    /// Records something about <paramref name="projection"/> that happened here.
    /// </summary>
    internal void Report<TSpace>(DiagnosticSeverity severity, IProjectionDefinition projection, string message, Plane<TSpace> extent)
      where TSpace : class, ISpace
    {
      var chain = Chain(PathRenderer.Through(projection));
      var (path, subject) = PathRenderer.Collapse(chain);

      Diagnostics.Add(new ProjectionDiagnostic(severity, subject, message, path, PathRenderer.RenderFull(chain), Locate(extent)));
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
        if (context.Projection is not IProjectionDefinition projection || PathRenderer.Skipped(projection))
          continue;

        chain.Insert(0, new PathNode(projection, context.Site, context.Index));
        deepest ??= projection;
      }

      if (failing is not null && !ReferenceEquals(deepest, failing))
        chain.Add(new PathNode(failing, Pending, null));

      return chain;
    }

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
