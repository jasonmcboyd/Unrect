using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The compute-legal binder as a standalone leaf: one body row read by
  /// <c>row =&gt; …</c>, the columns resolved through whatever ambient <see cref="LabelAxis.Column"/>
  /// scope a scope-introducer pushed. It owns no <see cref="TableView"/> and copies no resolution —
  /// a <see cref="TableRow"/> resolves by name through the context's label environment, so this leaf
  /// and a built-in <c>Table</c>'s rows share the one <c>Resolvable</c> path and cannot drift.
  /// <para>
  /// Its placement is a one-row band at the full width, exactly the band a built-in table hands each
  /// record; under a <c>VerticalRepeat</c> each occurrence takes one row and the repeat stops when
  /// there is no next row. The row's <see cref="TableRow.Index"/> is the occurrence number the
  /// enclosing repeat stamped on the context, so a decoupled record still numbers its rows.
  /// </para>
  /// </summary>
  internal sealed class RecordProjection<T> : ProjectionBase<T>
  {
    public RecordProjection(Func<TableRow, T> record, Placement placement)
      : base(placement)
      => Record = record ?? throw new ArgumentNullException(nameof(record));

    private Func<TableRow, T> Record { get; }

    public override string Description => "Record";

    public override ProjectionResult<T> Project(ICellValues extent, ProjectionContext context)
    {
      var strip = new CellStrip(extent, Orientation.Horizontal, context);
      var row = new TableRow(context.Ordinal ?? 0, strip, context);

      return new ProjectionResult<T>(Record(row), extent.Area.Size);
    }
  }
}
