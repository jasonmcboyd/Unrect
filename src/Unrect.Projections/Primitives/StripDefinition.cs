using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The strip leaf: one row or one column, handed to a lambda as a <see cref="CellStrip{TSpace}"/>
  /// of points, read at close. What <c>Row(cells =&gt; …)</c> and <c>Column(cells =&gt; …)</c> declare.
  /// </summary>
  /// <typeparam name="TSpace">The space the strip is read from.</typeparam>
  /// <typeparam name="T">What the lambda makes of it.</typeparam>
  public sealed class StripDefinition<TSpace, T> : CollectorNode<TSpace, T>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A one-line leaf — a row when <paramref name="orientation"/> is horizontal, a column when vertical — handed to <paramref name="project"/> as a <see cref="CellStrip{TSpace}"/>.
    /// <para>Fluent: <c>Row(cells =&gt; …)</c>.</para>
    /// </summary>
    /// <param name="orientation">Which way the strip runs.</param>
    /// <param name="project">What to make of the strip.</param>
    /// <param name="placement">Where the strip is.</param>
    /// <param name="description">The path segment, such as <c>Row</c> or <c>Column</c>.</param>
    public StripDefinition(Orientation orientation, Func<CellStrip<TSpace>, T> project, Placement<TSpace> placement, string description)
      : base(placement)
    {
      Orientation = orientation;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    private Orientation Orientation { get; }
    private Func<CellStrip<TSpace>, T> Projection { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <summary>A row strip is one row-span; a column strip is one column-span, held and re-driven under a row-major driver.</summary>
    public override Axes Axis => Orientation == Orientation.Horizontal ? Axes.Vertical : Axes.Horizontal;

    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      var size = extent.Extent;

      if (Orientation == Orientation.Horizontal && size.Height != 1)
        throw scope.Failure($"a Row must be exactly one row tall; this one is {size.Height} rows tall", extent);

      if (Orientation == Orientation.Vertical && size.Width != 1)
        throw scope.Failure($"a Column must be exactly one column wide; this one is {size.Width} columns wide", extent);

      try
      {
        return new Settlement<T>(Projection(new CellStrip<TSpace>(extent, Orientation, scope)), size);
      }
      catch (CellReadException failure)
      {
        throw scope.Reading(failure, extent);
      }
    }
  }
}
