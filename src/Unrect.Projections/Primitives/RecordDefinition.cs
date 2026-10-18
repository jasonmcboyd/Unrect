using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The compute-legal binder as a standalone leaf: one body row read by
  /// <c>row =&gt; …</c>, the columns resolved through whatever ambient <see cref="LabelAxis.Column"/>
  /// scope a scope-introducer pushed. It owns no <see cref="TableView{TSpace}"/> and copies no resolution —
  /// a <see cref="TableRow{TSpace}"/> resolves by name through the scope's label environment, so this leaf
  /// and a built-in <c>Table</c>'s rows share the one <c>Resolvable</c> path and cannot drift.
  /// <para>
  /// Its placement is a one-row band at the full width, exactly the band a built-in table hands each
  /// record; under a <c>VerticalRepeat</c> each occurrence takes one row and the repeat stops when
  /// there is no next row. The row's <see cref="TableRow{TSpace}.Index"/> is the occurrence number the
  /// enclosing repeat stamped on the scope, so a decoupled record still numbers its rows.
  /// </para>
  /// </summary>
  public sealed class RecordDefinition<TSpace, T> : CollectorNode<TSpace, T>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A one-row leaf that hands its row to <paramref name="record"/> as a <see cref="TableRow{TSpace}"/>, labels in scope and all.
    /// <para>Fluent: <c>Record(r =&gt; …)</c>.</para>
    /// </summary>
    /// <param name="record">What to make of the row.</param>
    /// <param name="placement">Where the row is.</param>
    public RecordDefinition(Func<TableRow<TSpace>, T> record, Placement<TSpace> placement)
      : base(placement)
      => Record = record ?? throw new ArgumentNullException(nameof(record));

    private Func<TableRow<TSpace>, T> Record { get; }

    /// <inheritdoc/>
    public override string Description => "Record";

    /// <inheritdoc/>
    public override Axes Axis => Axes.Vertical;

    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      var strip = new CellStrip<TSpace>(extent, Orientation.Horizontal, scope);
      var row = new TableRow<TSpace>(scope.Ordinal ?? 0, strip, scope);

      try
      {
        return new Settlement<T>(Record(row), extent.Extent);
      }
      catch (CellReadException failure)
      {
        throw scope.Reading(failure, extent);
      }
    }
  }
}
