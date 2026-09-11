using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Reads and consumes a table's header band, yielding the <see cref="LabelMap"/> of its columns —
  /// the "manufacture a map" primitive a scope-introducer then pushes for the body beneath it. It
  /// reuses the header parser <see cref="TableView"/> already carries, so its labels, ordinals and
  /// matching rule are byte-identical to a built-in <c>Table</c>'s by construction.
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
      // The throwaway TableView pushes a Column scope onto a context this projection discards — it
      // never streams bands, so nothing reads that scope. What it is here for is the header parse:
      // one shared code path with Table means the two cannot mint different labels for one header.
      var view = new TableView(extent, HeaderRows, context);

      return new ProjectionResult<LabelMap>(new LabelMap(view), extent.Area.Size);
    }
  }
}
