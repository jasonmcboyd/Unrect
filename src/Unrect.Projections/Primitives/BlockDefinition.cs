using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The block leaf: its whole region, handed to a lambda as a <see cref="CellBlock{TSpace}"/> of
  /// points, read at close. What <c>Range(block =&gt; …)</c> declares.
  /// </summary>
  /// <typeparam name="TSpace">The space the block is read from.</typeparam>
  /// <typeparam name="T">What the lambda makes of it.</typeparam>
  public sealed class BlockDefinition<TSpace, T> : CollectorNode<TSpace, T>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A leaf that hands the whole region its placement finds to <paramref name="project"/> as a <see cref="CellBlock{TSpace}"/>.
    /// <para>Fluent: <c>Range(block =&gt; …)</c>.</para>
    /// </summary>
    /// <param name="project">What to make of the block; a cell read that fails inside it is a located failure.</param>
    /// <param name="placement">Where the block is and how big; <see cref="Placement{TSpace}.Default"/> takes what it is handed.</param>
    /// <param name="description">The path segment this leaf renders as, such as <c>Range</c>.</param>
    public BlockDefinition(Func<CellBlock<TSpace>, T> project, Placement<TSpace> placement, string description)
      : base(placement)
    {
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    private Func<CellBlock<TSpace>, T> Projection { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <summary>A block lambda reads its whole extent at random, so it streams along no axis: held, then handed the region as one span.</summary>
    public override Axes Axis => Axes.None;

    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      T value;

      try
      {
        value = Projection(new CellBlock<TSpace>(extent, scope));
      }
      catch (CellReadException failure)
      {
        throw scope.Reading(failure, extent);
      }

      return new Settlement<T>(value, extent.Extent);
    }
  }
}
