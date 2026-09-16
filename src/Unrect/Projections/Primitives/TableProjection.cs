using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class TableProjection<TSpace, T> : ProjectionBase<TSpace, T>
    where TSpace : class, ISpace
  {
    public TableProjection(int headerRows, Func<TableView<TSpace>, T> project, Placement placement, string description)
      : base(placement)
    {
      HeaderRows = headerRows;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private int HeaderRows { get; }
    private Func<TableView<TSpace>, T> Projection { get; }

    public override string Description { get; }

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
