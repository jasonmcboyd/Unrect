using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Reads and consumes a table's header band, yielding the <see cref="LabelMap"/> of its columns —
  /// the "manufacture a map" primitive a scope-introducer then pushes for the body beneath it. The
  /// header is parsed through <see cref="LabelMap.FromHeader"/>, the one code path a built-in
  /// <c>Table</c> parses its header through too, so their labels, ordinals and matching rule are
  /// byte-identical by construction.
  /// </summary>
  internal sealed class ColumnLabelsProjection : ProjectionBase<LabelMap>
  {
    public ColumnLabelsProjection(int headerRows, Placement placement)
      : base(placement)
      => HeaderRows = headerRows;

    private int HeaderRows { get; }

    public override string Description => "ColumnLabels";

    public override ProjectionResult<LabelMap> Project(ISpace extent, ProjectionContext context)
    {
      var headerBand = extent.GetSubspace(new Offset(0, 0), new Area(BoundedSpace.WidthOf(extent), HeaderRows));
      var header = new CellStrip(headerBand, Orientation.Horizontal, context);

      return new ProjectionResult<LabelMap>(LabelMap.FromHeader(header, context), extent.Area.Size);
    }
  }
}
