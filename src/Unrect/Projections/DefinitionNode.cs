using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The base for every projection: a base for <em>construction</em>, not behaviour. It carries the
  /// <see cref="Annotations"/> every projection has and the one clone that changes them; what a
  /// projection does with its extent is its own, and what a modifier wraps it in is the modifier's.
  /// </summary>
  public abstract class DefinitionNode : IProjectionDefinition
  {
    private static readonly Child[] NoChildren = Array.Empty<Child>();

    /// <summary>
    /// Fixes where the projection sits. Every projection has a placement from the moment it exists,
    /// so the engine never has to ask whether one was declared.
    /// </summary>
    /// <param name="placement">Where this projection sits within the space it is handed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="placement"/> is null.</exception>
    private protected DefinitionNode(Placement placement)
    {
      Annotations = Annotations.Default.WithPlacement(placement);
    }

    /// <inheritdoc/>
    public Annotations Annotations { get; private set; }

    /// <inheritdoc/>
    public string? Name => Annotations.Name;

    /// <inheritdoc/>
    public Placement Placement => Annotations.Placement;

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <summary>No children by default; a composite overrides this to declare its own.</summary>
    public virtual IReadOnlyList<Child> Children => NoChildren;

    /// <summary>A level of the tree by default; only a wrapper overrides this to true.</summary>
    public virtual bool IsWrapper => false;

    /// <summary>The children's reach joined, by default; a node with a rule of its own joins it in.</summary>
    public virtual Reach Reach
    {
      get
      {
        var reach = Reach.None;

        foreach (var child in Children)
          reach = reach.Join(child.Definition.Reach);

        return reach;
      }
    }

    /// <summary>Either axis by default — a leaf's span is one cell whichever way it arrives; a shape with an orientation overrides this.</summary>
    public virtual Axes Axis => Axes.Either;

    /// <summary>
    /// How far back this node's own machine may still read, apart from its children's needs: none
    /// for a composite that reads nothing itself, every span it was fed for a collector that reads
    /// them at <c>Close</c>, its extent for a node that replays. The engine keeps a streamed
    /// source's rows from the oldest such need among the machines still open.
    /// </summary>
    internal virtual Reach Retains => Collects ? Reach.Extent : Reach.None;

    /// <summary>
    /// Why this node must see its whole extent before it runs, or null when nothing about it says
    /// so: a repeat whose separator has no per-span form. The engine holds such a node and drives
    /// it again along its own axis once its extent is known.
    /// </summary>
    internal virtual string? Holds => null;

    /// <summary>
    /// Whether this node reads its region whole once the spans it was fed are known — a cell, a
    /// strip, a block, a header, a table view — rather than consuming spans as they arrive. A
    /// collector under a declared rule is driven along whichever axis the rule runs, since the rule
    /// bounds it; left to bound itself it is held.
    /// </summary>
    internal virtual bool Collects => false;

    /// <summary>Children are the whole truth by default; a layout overrides this to say why they are not.</summary>
    public virtual string? Opacity => null;

    /// <inheritdoc/>
    public string? UnitName => Annotations.UnitName;

    /// <inheritdoc/>
    public bool IsScaffolding => Annotations.IsScaffolding;

    /// <summary>
    /// This projection carrying <paramref name="annotations"/> — a copy, of this same type. The one
    /// way any annotation changes: every modifier that names, labels, marks or places builds the
    /// record it wants and comes through here. A widening that has to build a new projection rather
    /// than clone one — <c>OrBlank</c>, which changes the result type — hands the new leaf the old
    /// one's record the same way, so a leaf does not silently change its own path by being made
    /// tolerant.
    /// </summary>
    internal DefinitionNode With(Annotations annotations)
    {
      var clone = (DefinitionNode)MemberwiseClone();
      clone.Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
      return clone;
    }

  }

  /// <summary>
  /// The base for every projection that reads a <typeparamref name="TResult"/>. Subclasses must be
  /// immutable field bags: the clone-returning operations copy the projection wholesale, so a
  /// subclass that mutates state after construction breaks the guarantee that one projection can be
  /// applied to many spaces concurrently.
  /// </summary>
  /// <typeparam name="TSpace">The space this projection is written over.</typeparam>
  /// <typeparam name="TResult">What projecting this projection's extent produces.</typeparam>
  public abstract class DefinitionNode<TSpace, TResult> : DefinitionNode, IProjectionDefinition<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <inheritdoc cref="DefinitionNode(Placement)"/>
    /// <remarks>
    /// Not <c>protected</c>: only this library may derive. The modifier surface casts its result
    /// back to the receiver's own type on the strength of "every projection is one of ours", and an
    /// externally authored subclass would be the one way to make that cast fail at runtime while
    /// compiling cleanly. Closed at the projection rename (owner decision).
    /// </remarks>
    /// <param name="placement">Where this projection sits within the space it is handed.</param>
    private protected DefinitionNode(Placement placement)
      : base(placement)
    {
    }

    /// <inheritdoc/>
    public abstract IProjector<TSpace, TResult> Build(ProjectorScope<TSpace> scope);

    /// <summary>
    /// Reads <paramref name="extent"/> whole — a collector's own act, once the spans it was fed
    /// are known: a cell, a strip, a block, a header, a table view. A node that is not a collector
    /// builds a machine instead and is never asked.
    /// </summary>
    internal virtual Settlement<TResult> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
      => throw new InvalidOperationException($"{Description} is not read whole; it builds a machine.");

    /// <inheritdoc/>
    IProjectionDefinition<TSpace, TResult> IProjectionDefinition<TSpace, TResult>.With(Annotations annotations)
      => (IProjectionDefinition<TSpace, TResult>)With(annotations);
  }
}
