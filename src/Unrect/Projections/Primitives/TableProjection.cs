using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class TableProjection<T> : ProjectionBase<T>
  {
    public TableProjection(int headerRows, Func<TableView, T> project, Placement placement, string description, IProjection? eachRow = null)
      : base(placement)
    {
      HeaderRows = headerRows;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
      Children = eachRow is null ? Array.Empty<IProjection>() : new[] { eachRow };
    }

    private int HeaderRows { get; }
    private Func<TableView, T> Projection { get; }

    public override string Description { get; }

    /// <summary>
    /// The row projection, where the table has one. The lambda rungs have none to show — what they
    /// read is knowable only by running them — so they are a leaf to tooling, as they have always
    /// been.
    /// </summary>
    public override IReadOnlyList<IProjection> Children { get; }

    public override ProjectionResult<T> Project(ISpace extent, ProjectionContext context)
    {
      // "Is there a row for the header" rather than "how tall are you": the same question of a
      // measured extent, and one row rather than all of them where the height is still being
      // discovered. Asking it the other way would settle every table's bound before it read a cell.
      if (HeaderRows > 0 && (BoundedSpace.WidthOf(extent) == 0 || !BoundedSpace.HasRow(extent, 0)))
        throw context.Failure("a header row was declared but the table's extent is empty", extent);

      var value = Projection(new TableView(extent, HeaderRows, context));

      // The extent is measured after the projection has run, never before — a declared area is
      // consumed in full, so this is where a bound the projection did not exhaust is settled, and
      // it is the same moment the engine would have settled it.
      return new ProjectionResult<T>(value, extent.Area.Size);
    }
  }
}
