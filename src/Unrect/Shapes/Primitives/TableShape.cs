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
      if (HeaderRows > 0 && (extent.Area.Height == 0 || extent.Area.Width == 0))
        throw context.Failure("a header row was declared but the table's extent is empty", extent);

      return new ShapeResult<T>(Projection(new TableView(extent, HeaderRows, context)), extent.Area.Size);
    }
  }
}
