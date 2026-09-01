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
      => new ShapeResult<T>(Projection(new CellBlock(extent, context.Origin)), extent.Area.Size);
  }
}
