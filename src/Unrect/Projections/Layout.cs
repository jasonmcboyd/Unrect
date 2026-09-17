using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Declares a layout: records each child with <see cref="LayoutCursor{TSpace}.Next{T}"/>, in
  /// the order they appear on the sheet, and closes with
  /// <see cref="LayoutCursor{TSpace}.Build{TResult}"/>, which is the only way to hand back a
  /// <see cref="Layout{TSpace, TResult}"/> — so a lambda that forgets to build does not compile.
  /// </summary>
  /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
  /// <typeparam name="TResult">What the layout builds from what its children read.</typeparam>
  /// <param name="cursor">The cursor the layout declares its children with.</param>
  public delegate Layout<TSpace, TResult> LayoutDeclaration<TSpace, TResult>(LayoutCursor<TSpace> cursor)
    where TSpace : class, ISpace;

  /// <summary>
  /// A closed layout: its children in order, each in a slot, and the combiner that builds the
  /// result from what they read. What <c>Build</c> hands back and a flow or overlay is made from;
  /// it has no members of its own to call.
  /// </summary>
  /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
  /// <typeparam name="TResult">What the layout builds from what its children read.</typeparam>
  public sealed class Layout<TSpace, TResult>
    where TSpace : class, ISpace
  {
    internal Layout(LayoutBuilder<TSpace> builder, Child[] children, LayoutRunner<TSpace>[] runners, Func<Reading, TResult> combine)
    {
      Builder = builder;
      Children = children;
      Runners = runners;
      Combine = combine;
    }

    /// <summary>The identity a slot and a reading agree on.</summary>
    internal LayoutBuilder<TSpace> Builder { get; }

    internal IReadOnlyList<Child> Children { get; }

    internal IReadOnlyList<LayoutRunner<TSpace>> Runners { get; }

    internal Func<Reading, TResult> Combine { get; }
  }

  /// <summary>
  /// What a cursor writes to: the children recorded so far, and the one-way door <c>Build</c>
  /// closes. Lives only for the declaration lambda; the layout it hands back is the immutable
  /// record.
  /// </summary>
  internal sealed class LayoutBuilder<TSpace>
    where TSpace : class, ISpace
  {
    private const string Outside = "A layout cursor cannot be used outside the layout that created it";

    /// <summary>
    /// Being outside a layout covers two different bugs, so the messages name which one: a cursor
    /// that never had a layout (<c>default</c>), and one used after its layout was built.
    /// </summary>
    internal const string NoLayout = Outside + "; this one never had a layout.";

    /// <inheritdoc cref="NoLayout"/>
    internal const string AlreadyBuilt = Outside + "; this one has already been built.";

    private readonly List<Child> _children = new List<Child>();
    private readonly List<LayoutRunner<TSpace>> _runners = new List<LayoutRunner<TSpace>>();
    private bool _built;

    private LayoutBuilder(string noun) => Noun = noun;

    /// <summary>What to call the layout in a refusal — "a flow", "an overlay".</summary>
    private string Noun { get; }

    /// <summary>
    /// Runs <paramref name="declare"/> once, now, and hands back the layout it built: the one
    /// moment a layout lambda ever runs. Refuses a lambda that returned nothing, or a layout some
    /// other cursor built.
    /// </summary>
    public static Layout<TSpace, TResult> Declare<TResult>(LayoutDeclaration<TSpace, TResult> declare, string noun, string parameter)
    {
      if (declare is null)
        throw new ArgumentNullException(parameter);

      var builder = new LayoutBuilder<TSpace>(noun);
      var layout = declare(new LayoutCursor<TSpace>(builder));

      if (layout is null)
        throw new ArgumentException("The layout lambda returned null; return the cursor's own Build(…).", parameter);

      if (!ReferenceEquals(layout.Builder, builder))
        throw new ArgumentException("The layout lambda returned a layout another cursor built; return this cursor's own Build(…).", parameter);

      return layout;
    }

    public Slot<T> Next<T>(IProjectionDefinition<TSpace, T> projection, string? declared)
    {
      if (_built)
        throw new InvalidOperationException(AlreadyBuilt);

      var index = _children.Count;

      // A null projection is a hole in the declaration, refused where the declaration is written.
      if (projection is null)
        throw new ArgumentNullException(nameof(projection), $"a null projection was declared as child {index + 1}");

      _children.Add(new Child(projection, UseSite.From(declared, index + 1)));
      _runners.Add(new LayoutRunner<TSpace, T>(projection));

      return new Slot<T>(this, index);
    }

    public Layout<TSpace, TResult> Build<TResult>(Func<Reading, TResult> combine)
    {
      if (_built)
        throw new InvalidOperationException(AlreadyBuilt);

      if (combine is null)
        throw new ArgumentNullException(nameof(combine));

      // A layout that declared nothing would match anything, describe nothing, and quietly end an
      // enclosing repetition by consuming nothing. That is a bug in the declaration, so it is
      // refused where the declaration is written.
      if (_children.Count == 0)
        throw new InvalidOperationException($"{Noun} must declare at least one projection; this one called Next zero times");

      _built = true;

      return new Layout<TSpace, TResult>(this, _children.ToArray(), _runners.ToArray(), combine);
    }
  }

  /// <summary>
  /// One child of a layout, applied with its result type known — what lets a flow's state take a
  /// child generically from an edge that holds it non-generically.
  /// </summary>
  internal abstract class LayoutRunner<TSpace>
    where TSpace : class, ISpace
  {
    public abstract object? Apply(LayoutState<TSpace> state, UseSite site);
  }

  internal sealed class LayoutRunner<TSpace, T> : LayoutRunner<TSpace>
    where TSpace : class, ISpace
  {
    public LayoutRunner(IProjectionDefinition<TSpace, T> projection) => Projection = projection;

    private IProjectionDefinition<TSpace, T> Projection { get; }

    public override object? Apply(LayoutState<TSpace> state, UseSite site) => state.Next(Projection, site);
  }
}
