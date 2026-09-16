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
  internal sealed class FieldProjection<TSpace> : ProjectionBase<TSpace, Point<TSpace>>
    where TSpace : class, ISpace
  {
    public FieldProjection(string label, Placement placement)
      : base(placement)
    {
      Label = label;
      Match = CellMatching.LabelEquals(label);
    }

    private string Label { get; }
    private Func<Point<ISpace>, bool> Match { get; }

    public override string Description => $"Field(\"{Label}\")";

    public override ProjectionResult<Point<TSpace>> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      // Normally unreachable — the factory fixes the extent at 2x1 — but reachable the moment a
      // caller replaces the placement (.Sized, a field inside a declared frame), and it is the half
      // of this leaf a writer would satisfy: the writer emits the pair, the reader verifies it.
      if (size.Width != 2 || size.Height != 1)
        throw context.Failure(
          $"a Field must be two cells wide and one row tall; this one is {size.Width}x{size.Height}", extent);

      if (!Match(extent.Erased()[0, 0]))
        throw context.Failure(
          $"expected a label reading '{Label}' here, but this cell {Describe(extent[0, 0])}",
          extent);

      return new ProjectionResult<Point<TSpace>>(extent[1, 0], size);
    }

    /// <summary>
    /// What the cell that should have carried the label does instead, in the canonical vocabulary: a
    /// cell that says a word of its own quotes it, and anything else says what it renders as.
    /// </summary>
    private static string Describe(Point<TSpace> cell)
      => cell.AsText() is not string text ? "is blank"
       : cell.IsText ? $"reads '{text}'"
       : $"renders as '{text}'";
  }
}
