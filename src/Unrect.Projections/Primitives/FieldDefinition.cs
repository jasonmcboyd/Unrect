using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// One labelled pair: a label cell asserted against the declared label, and the value cell
  /// immediately to its right. Two wide, one tall, and nothing else — a wider value region or a gap
  /// would be a different shape, and this one says which it is.
  /// <para>
  /// The value is handed back as the cell itself: the labels are the structure, the values are data,
  /// so a blank value is a blank cell rather than a failure.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space the pair's cells belong to.</typeparam>
  public sealed class FieldDefinition<TSpace> : CollectorNode<TSpace, Point<TSpace>>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A two-cell leaf: a label on the left that must say <paramref name="label"/>, and the cell beside it, which is the value.
    /// <para>Fluent: <c>Field("EIN")</c>.</para>
    /// </summary>
    /// <param name="label">What the left cell must say; refused when null or empty.</param>
    /// <param name="placement">Where the pair is; the fluent spelling places it two cells wide, one row tall.</param>
    public FieldDefinition(string label, Placement<TSpace> placement)
      : base(placement)
    {
      if (string.IsNullOrEmpty(label))
        throw new ArgumentException("A field label must say something.", nameof(label));

      Label = label;
      Match = CellMatching.LabelEquals<TSpace>(label);
    }

    private string Label { get; }
    private Func<Point<TSpace>, bool> Match { get; }

    /// <inheritdoc/>
    public override string Description => $"Field(\"{Label}\")";

    /// <inheritdoc/>
    public override Axes Axis => Axes.Vertical;

    internal override Settlement<Point<TSpace>> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      var size = extent.Extent;

      // Normally unreachable — the factory fixes the extent at 2x1 — but reachable the moment a
      // caller replaces the placement (.Sized, a field inside a declared frame), and it is the half
      // of this leaf a writer would satisfy: the writer emits the pair, the reader verifies it.
      if (size.Width != 2 || size.Height != 1)
        throw scope.Failure(
          $"a Field must be two cells wide and one row tall; this one is {size.Width}x{size.Height}", extent);

      if (!Match(extent[0, 0]))
        throw scope.Failure(
          $"expected a label reading '{Label}' here, but this cell {Describe(extent[0, 0])}",
          extent);

      return new Settlement<Point<TSpace>>(extent[1, 0], size);
    }

    /// <summary>
    /// What the cell that should have carried the label does instead, in the canonical vocabulary: a
    /// cell that says a word of its own quotes it, and anything else says what it renders as.
    /// </summary>
    private static string Describe(Point<TSpace> cell)
      => cell.IsBlank() ? "is blank" : $"says '{cell.AsText()}'";
  }
}
