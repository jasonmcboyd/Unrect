using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  internal sealed class BlockShape<T> : ShapeBase<T>
  {
    public BlockShape(Func<CellBlock, T> project, Placement placement, string description)
      : base(placement)
    {
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private Func<CellBlock, T> Projection { get; }

    public override string Description { get; }

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var value = Projection(new CellBlock(extent, context.Origin));

      // The extent is measured after the projection has run, never before: on a bound still being
      // discovered, asking first would settle it before the projection had read a row.
      return new ShapeResult<T>(value, extent.Area.Size);
    }
  }
}
