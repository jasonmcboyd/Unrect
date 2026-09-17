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

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      // "Is there a row for the header" rather than "how tall are you": the same question of a
      // measured extent, and one row rather than all of them where the height is still being
      // discovered. Asking it the other way would settle every table's bound before it read a cell.
      if (HeaderRows > 0 && (extent.Width == 0 || !extent.HasRow(0)))
        throw context.Failure("a header row was declared but the table's extent is empty", extent);

      T value;

      try
      {
        value = Projection(new TableView<TSpace>(extent, HeaderRows, context));
      }
      catch (CellReadException failure)
      {
        throw context.Reading(failure, extent);
      }

      // The extent is measured after the projection has run, never before — a declared area is
      // consumed in full, so this is where a bound the projection did not exhaust is settled, and
      // it is the same moment the engine would have settled it.
      return new ProjectionResult<T>(value, extent.Area.Size);
    }
  }
}
