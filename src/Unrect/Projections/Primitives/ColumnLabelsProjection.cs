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
  internal sealed class ColumnLabelsProjection<TSpace> : ProjectionBase<TSpace, LabelMap>
    where TSpace : class, ISpace
  {
    public ColumnLabelsProjection(int headerRows, Placement placement)
      : base(placement)
      => HeaderRows = headerRows;

    private int HeaderRows { get; }

    public override string Description => "ColumnLabels";

    public override ProjectionResult<LabelMap> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var width = extent.Width;

      // "Is there a row for the header" rather than "how tall are you": the same question of a
      // measured extent, and only as far as the header reaches where the height is still being
      // discovered. Without it, cutting the band off an empty extent throws a bare
      // OutOfBoundsException where an absorbable failure belongs.
      if (width == 0 || !extent.HasRow(HeaderRows - 1))
        throw context.Failure("a header row was declared but the table's extent is empty", extent);

      var headerBand = extent.Cut(new Offset(0, 0), new Area(width, HeaderRows));
      var header = new CellStrip<TSpace>(headerBand, Orientation.Horizontal, context);

      return new ProjectionResult<LabelMap>(LabelMap.FromHeader(header), new Size(width, HeaderRows));
    }
  }
}
