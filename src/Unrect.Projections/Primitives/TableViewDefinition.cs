using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The table-view leaf: its whole region, the header rows parsed into labels, handed to a lambda
  /// as a <see cref="TableView{TSpace}"/>, read at close. What <c>Table(view =&gt; …)</c> declares.
  /// </summary>
  /// <typeparam name="TSpace">The space the table is read from.</typeparam>
  /// <typeparam name="T">What the lambda makes of it.</typeparam>
  public sealed class TableViewDefinition<TSpace, T> : CollectorNode<TSpace, T>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A leaf that hands its whole region to <paramref name="project"/> as a <see cref="TableView{TSpace}"/>, the first <paramref name="headerRows"/> rows read as the header.
    /// <para>Fluent: <c>Table(view =&gt; …)</c>.</para>
    /// </summary>
    /// <param name="headerRows">How many rows are the header; zero for none.</param>
    /// <param name="project">What to make of the table.</param>
    /// <param name="placement">Where the table is.</param>
    /// <param name="description">The path segment, such as <c>Table</c>.</param>
    /// <param name="opacity">Why this node's children are not visible to tooling, when its row projection is built from the header it read; null otherwise.</param>
    public TableViewDefinition(int headerRows, Func<TableView<TSpace>, T> project, Placement<TSpace> placement, string description, string? opacity = null)
      : base(placement)
    {
      HeaderRows = headerRows >= 0
        ? headerRows
        : throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "A table cannot have a negative number of header rows.");
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description ?? throw new ArgumentNullException(nameof(description));
      Opacity = opacity;
    }

    private int HeaderRows { get; }
    private Func<TableView<TSpace>, T> Projection { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <summary>
    /// Null for a lambda rung, which is a leaf to tooling — with a lambda in the slot there was
    /// never a child to declare. The bind rung says why its child is missing: it is built from the
    /// header's captions, so it is known only once a header is read.
    /// </summary>
    public override string? Opacity { get; }

    /// <summary>A view lambda reads its table at random, so it streams along no axis: held, then handed the region as one span.</summary>
    public override Axes Axis => Axes.None;

    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      if (HeaderRows > 0 && (extent.Width == 0 || extent.Height == 0))
        throw scope.Failure("a header row was declared but the table's extent is empty", extent);

      T value;

      try
      {
        value = Projection(new TableView<TSpace>(extent, HeaderRows, scope));
      }
      catch (CellReadException failure)
      {
        throw scope.Reading(failure, extent);
      }

      return new Settlement<T>(value, extent.Extent);
    }
  }
}
