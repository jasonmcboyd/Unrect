using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  internal sealed class CellShape<T> : ShapeBase<T>
  {
    public CellShape(Func<CellValue, T> project, Placement placement)
      : base(placement)
    {
      Projection = project ?? throw new ArgumentNullException(nameof(project));
    }

    private Func<CellValue, T> Projection { get; }

    public override string Description => "Cell";

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"a Cell must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      return new ShapeResult<T>(Projection(extent[0, 0]), size);
    }
  }
}
