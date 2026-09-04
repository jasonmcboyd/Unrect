using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  internal sealed class TableShape<T> : ShapeBase<T>
  {
    public TableShape(int headerRows, Func<TableView, T> project, Placement placement, string description)
      : base(placement)
    {
      HeaderRows = headerRows;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private int HeaderRows { get; }
    private Func<TableView, T> Projection { get; }

    public override string Description { get; }

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      // "Is there a row for the header" rather than "how tall are you": the same question of a
      // measured extent, and one row rather than all of them where the height is still being
      // discovered. Asking it the other way would settle every table's bound before it read a cell.
      if (HeaderRows > 0 && (BoundedSpace.WidthOf(extent) == 0 || !BoundedSpace.HasRow(extent, 0)))
        throw context.Failure("a header row was declared but the table's extent is empty", extent);

      var value = Projection(new TableView(extent, HeaderRows, context));

      // The extent is measured after the projection has run, never before — a declared area is
      // consumed in full, so this is where a bound the projection did not exhaust is settled, and it
      // is the same moment the engine would have settled it.
      return new ShapeResult<T>(value, extent.Area.Size);
    }
  }
}
