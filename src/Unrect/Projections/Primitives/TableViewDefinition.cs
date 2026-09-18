using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class TableViewDefinition<TSpace, T> : DefinitionNode<TSpace, T>
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

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, T>(this, scope, 1);

    internal override bool Collects => true;


    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      // "Is there a row for the header" rather than "how tall are you": the same question of a
      // measured extent, and one row rather than all of them where the height is still being
      // discovered. Asking it the other way would settle every table's bound before it read a cell.
      if (HeaderRows > 0 && (extent.Width == 0 || !extent.HasRow(0)))
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

      // The extent is measured after the projection has run, never before — a declared area is
      // consumed in full, so this is where a bound the projection did not exhaust is settled, and
      // it is the same moment the engine would have settled it.
      return new Settlement<T>(value, extent.Area.Size);
    }
  }
}
