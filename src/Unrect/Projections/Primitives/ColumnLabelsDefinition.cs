using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Reads and consumes a table's header band, yielding the <see cref="LabelMap"/> of its columns —
  /// the "manufacture a map" primitive a scope-introducer then pushes for the body beneath it. The
  /// header is parsed through <see cref="LabelMap.FromHeader"/>, the one code path a built-in
  /// <c>Table</c> parses its header through too, so their labels, ordinals and matching rule are
  /// byte-identical by construction.
  /// <para>
  /// It takes the width of the band it is handed and consumes exactly its header rows. The width is
  /// the table's — what <c>TableView{TSpace}.ColumnCount</c> has always been — rather than a rule of its
  /// own, so a header and the body beneath it describe the same columns.
  /// </para>
  /// </summary>
  internal sealed class ColumnLabelsDefinition<TSpace> : CollectorNode<TSpace, LabelMap>
    where TSpace : class, ISpace
  {
    public ColumnLabelsDefinition(int headerRows, Placement placement)
      : base(placement)
      => HeaderRows = headerRows;

    private int HeaderRows { get; }

    public override string Description => "ColumnLabels";

    public override Axes Axis => Axes.Vertical;

    internal override int SpanCount => HeaderRows;

    internal override Settlement<LabelMap> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      var width = extent.Width;

      // Checked here so an extent with no room for the header is an absorbable failure rather than
      // the bare OutOfBoundsException cutting the band would throw.
      if (width == 0 || extent.Area.Height < HeaderRows)
        throw scope.Failure("a header row was declared but the table's extent is empty", extent);

      var headerBand = extent.Slice(new Offset(0, 0), new Area(width, HeaderRows));
      var header = new CellStrip<TSpace>(headerBand, Orientation.Horizontal, scope);

      return new Settlement<LabelMap>(LabelMap.FromHeader(header), new Size(width, HeaderRows));
    }
  }
}
