using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Where a scope is in the declaration tree: the definition it is inside, its occurrence index,
  /// the use site that labels it, the site waiting for the next descent, the ambient labels, and
  /// the chain of positions above it. Immutable — every move is a new position over the same
  /// parent — so a rendered path is a walk up the chain and nothing accumulates on the way down.
  /// </summary>
  internal sealed class TreePosition
  {
    private TreePosition(
      TreePosition? parent,
      IProjectionDefinition? projection,
      int? index,
      UseSite site,
      UseSite pending,
      LabelScope? labels,
      int? ordinal,
      IProjectionDefinition? blame)
    {
      Parent = parent;
      Projection = projection;
      Index = index;
      Site = site;
      Pending = pending;
      Labels = labels;
      Ordinal = ordinal;
      Blame = blame;
    }

    /// <summary>The position a run starts from: no definition yet, no path.</summary>
    internal static TreePosition Root => new TreePosition(null, null, null, default, default, null, null, null);

    private TreePosition? Parent { get; }

    /// <summary>The definition this position is inside, or null at the root.</summary>
    internal IProjectionDefinition? Projection { get; }

    /// <summary>
    /// Who to blame at the root when nothing was entered — see <see cref="Blaming"/>. Null
    /// everywhere else, because everywhere else there is a <see cref="Projection"/>.
    /// </summary>
    private IProjectionDefinition? Blame { get; }

    /// <summary>Which occurrence of the definition this is, where that is meaningful (inside a repeat or a tiler).</summary>
    internal int? Index { get; }

    /// <summary>
    /// The occurrence number the nearest enclosing repeat is on, or null outside one. Unlike
    /// <see cref="Index"/> — which names the path segment and is cleared on <see cref="Descend"/> so
    /// a repeat's item does not inherit its parent's ordinal into its own children — this is copied
    /// unchanged through <see cref="Descend"/> and so persists into the whole subtree the repeat
    /// manufactures. That is what lets a decoupled record recover the body row it is projecting.
    /// </summary>
    internal int? Ordinal { get; }

    /// <summary>Where this position's own definition was used — the label its segment renders with.</summary>
    private UseSite Site { get; }

    /// <summary>The use site waiting for the next <see cref="Descend"/>, set by whoever is about to start a child.</summary>
    private UseSite Pending { get; }

    /// <summary>
    /// The ambient label environment: a stack of <see cref="LabelScope"/> cons-cells a labelling
    /// definition pushes for its declaration subtree, or null where none is in scope. Copied
    /// unchanged through every move, so a label reaches every descendant.
    /// </summary>
    private LabelScope? Labels { get; }

    /// <summary>
    /// Pushes <paramref name="source"/> as the nearest set of labels along <paramref name="axis"/>,
    /// with <paramref name="captureOrigin"/> — the origin of the region the labels are APPLIED to —
    /// as the frame their ordinals are relative to.
    /// </summary>
    internal TreePosition PushLabels(LabelAxis axis, ILabelSource source, Offset captureOrigin)
      => new TreePosition(Parent, Projection, Index, Site, Pending, new LabelScope(axis, source, captureOrigin, Labels), Ordinal, Blame);

    /// <summary>The nearest labels along <paramref name="axis"/>, or null when none is in scope; an inner scope shadows an outer one on the same axis.</summary>
    internal LabelScope? NearestLabels(LabelAxis axis)
    {
      for (var scope = Labels; scope is not null; scope = scope.Outer)
        if (scope.Axis == axis)
          return scope;

      return null;
    }

    /// <summary>Enters <paramref name="projection"/>, which claims whatever use site was waiting for it.</summary>
    internal TreePosition Descend(IProjectionDefinition projection)
      => new TreePosition(this, projection, null, Pending, default, Labels, Ordinal, null);

    /// <summary>
    /// The root, told which definition is about to be applied to it — the one it may blame for a
    /// failure raised before anything has been entered. A wrapper the path skips is not descended
    /// into, so at the root it would otherwise have nothing to report against. Anywhere but the
    /// root this is the identity.
    /// </summary>
    internal TreePosition Blaming(IProjectionDefinition projection)
      => Projection is null
        ? new TreePosition(Parent, null, Index, Site, Pending, Labels, Ordinal, projection)
        : this;

    /// <summary>Declares where the next child was written, for it to claim on the way in.</summary>
    internal TreePosition WithUseSite(UseSite site)
      => new TreePosition(Parent, Projection, Index, Site, site, Labels, Ordinal, Blame);

    internal TreePosition WithIndex(int index)
      => new TreePosition(Parent, Projection, index, Site, Pending, Labels, Ordinal, Blame);

    /// <summary>Stamps the occurrence a repeat is on, which survives the descent into the item where <see cref="WithIndex"/> does not.</summary>
    internal TreePosition WithOrdinal(int ordinal)
      => new TreePosition(Parent, Projection, Index, Site, Pending, Labels, ordinal, Blame);

    /// <summary>The definition a failure reported from here is about: the one this position is inside, or at the root the one being applied.</summary>
    internal IProjectionDefinition Blamed()
      => Projection
        ?? Blame
        ?? throw new System.InvalidOperationException("The root has no definition to blame; report failures from within a machine.");

    /// <summary>
    /// The chain of enclosing definitions, root to leaf, ending at <paramref name="failing"/> — a
    /// child of this position when a definition fails before it is descended into. Wrappers the
    /// path skips say nothing about themselves and are left out.
    /// </summary>
    internal List<PathNode> Chain(IProjectionDefinition? failing)
    {
      var chain = new List<PathNode>();
      IProjectionDefinition? deepest = null;

      for (var position = this; position is not null; position = position.Parent)
      {
        if (position.Projection is not IProjectionDefinition projection || PathRenderer.Skipped(projection))
          continue;

        chain.Insert(0, new PathNode(projection, position.Site, position.Index));
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
  /// captured at, and the scope it shadows. Immutable — a cons-cell in the position's label stack.
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

    /// <summary>The origin of the region the labels were read in, in the root space's own coordinates.</summary>
    public Offset CaptureOrigin { get; }

    /// <summary>The scope this one shadows, or null at the bottom of the stack.</summary>
    public LabelScope? Outer { get; }
  }
}
