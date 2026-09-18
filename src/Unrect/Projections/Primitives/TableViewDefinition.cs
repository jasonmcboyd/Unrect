using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class TableViewDefinition<TSpace, T> : CollectorNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public TableViewDefinition(int headerRows, Func<TableView<TSpace>, T> project, Placement placement, string description, string? opacity = null)
      : base(placement)
    {
      HeaderRows = headerRows;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
      Opacity = opacity;
    }

    private int HeaderRows { get; }
    private Func<TableView<TSpace>, T> Projection { get; }

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
      if (HeaderRows > 0 && (extent.Width == 0 || extent.Area.Height == 0))
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

      return new Settlement<T>(value, extent.Area.Size);
    }
  }
}
