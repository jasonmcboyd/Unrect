using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The compute-legal binder as a standalone leaf: one body row read by
  /// <c>row =&gt; …</c>, the columns resolved through whatever ambient <see cref="LabelAxis.Column"/>
  /// scope a scope-introducer pushed. It owns no <see cref="TableView{TSpace}"/> and copies no resolution —
  /// a <see cref="TableRow{TSpace}"/> resolves by name through the context's label environment, so this leaf
  /// and a built-in <c>Table</c>'s rows share the one <c>Resolvable</c> path and cannot drift.
  /// <para>
  /// Its placement is a one-row band at the full width, exactly the band a built-in table hands each
  /// record; under a <c>VerticalRepeat</c> each occurrence takes one row and the repeat stops when
  /// there is no next row. The row's <see cref="TableRow{TSpace}.Index"/> is the occurrence number the
  /// enclosing repeat stamped on the context, so a decoupled record still numbers its rows.
  /// </para>
  /// </summary>
  internal sealed class RecordDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public RecordDefinition(Func<TableRow<TSpace>, T> record, Placement placement)
      : base(placement)
      => Record = record ?? throw new ArgumentNullException(nameof(record));

    private Func<TableRow<TSpace>, T> Record { get; }

    public override string Description => "Record";

    public override Axes Axis => Axes.Vertical;

    public override IProjector<TSpace, T> Start(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, T>(this, scope, 1);

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var strip = new CellStrip<TSpace>(extent, Orientation.Horizontal, context);
      var row = new TableRow<TSpace>(context.Ordinal ?? 0, strip, context);

      try
      {
        return new ProjectionResult<T>(Record(row), extent.Area.Size);
      }
      catch (CellReadException failure)
      {
        throw context.Reading(failure, extent);
      }
    }
  }
}
