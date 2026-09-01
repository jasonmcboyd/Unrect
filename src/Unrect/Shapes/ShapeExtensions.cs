using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// Application, naming, and placement modifiers.
  /// <para>
  /// The movement modifiers — <c>Down</c>, <c>Right</c>, <c>AfterBlankRows</c>,
  /// <c>AfterBlankColumns</c> — <em>compose</em>: each one starts from where the shape already sits,
  /// so <c>.Right(9).Down(1)</c> anchors at column 9, row 1, and <c>Table(...).Down(2)</c> means
  /// "past the blank rows, then two more".
  /// </para>
  /// <para>
  /// <c>After</c> <em>replaces</em> the offset outright — the "put it exactly here" spelling, and
  /// the way to discard a default. <c>Sized</c> replaces too, since extents do not stack.
  /// </para>
  /// </summary>
  public static partial class ShapeExtensions
  {
    /// <summary>
    /// Decomposes <paramref name="space"/> and projects it in one call. The shape's own placement
    /// is applied here too, exactly as it would be nested inside another shape.
    /// <para>
    /// Coordinates in failures are relative to <paramref name="space"/>, so a <c>Map</c> called
    /// from inside another shape's projection restarts them and reports positions relative to its
    /// own space. Compose shapes instead of nesting <c>Map</c> calls wherever you can.
    /// </para>
    /// </summary>
    public static TResult Map<TResult>(this IShape<TResult> shape, ISpace space) => shape.Apply(space).Value;

    /// <summary>
    /// <see cref="Map{TResult}"/> plus where the shape landed and how much it consumed.
    /// </summary>
    public static AppliedResult<TResult> Apply<TResult>(this IShape<TResult> shape, ISpace space)
    {
      if (shape is null)
        throw new ArgumentNullException(nameof(shape));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return ShapeEngine.Apply(shape, space, ShapeContext.Root(space));
    }

    /// <summary>Labels the shape, so failures and diagnostics say <paramref name="name"/>.</summary>
    public static IShape<T> Named<T>(this IShape<T> shape, string name) => NotNull(shape).WithName(name);

    /// <summary>
    /// Positions the shape at <paramref name="offset"/>, <em>replacing</em> any offset it had —
    /// including a default, which is how a <c>Table</c> is told not to skip its blank rows.
    /// </summary>
    public static IShape<T> After<T>(this IShape<T> shape, IOffsetStrategy offset)
      => NotNull(shape).WithPlacement(shape.Placement.WithOffset(offset));

    /// <summary>Moves the shape on past the blank rows in front of it.</summary>
    public static IShape<T> AfterBlankRows<T>(this IShape<T> shape) => Move(shape, OffsetStrategies.SkipBlankRows());

    /// <summary>Moves the shape on past the blank columns in front of it.</summary>
    public static IShape<T> AfterBlankColumns<T>(this IShape<T> shape) => Move(shape, OffsetStrategies.SkipBlankColumns());

    /// <summary>Moves the shape on <paramref name="rows"/> rows down from where it sits.</summary>
    public static IShape<T> Down<T>(this IShape<T> shape, int rows)
      => Move(shape, OffsetStrategies.ExplicitOffset(0, NotNegative(rows, nameof(rows))));

    /// <summary>
    /// Moves the shape on <paramref name="columns"/> columns right from where it sits.
    /// </summary>
    public static IShape<T> Right<T>(this IShape<T> shape, int columns)
      => Move(shape, OffsetStrategies.ExplicitOffset(NotNegative(columns, nameof(columns)), 0));

    /// <summary>
    /// Declares the shape's extent, replacing whatever it had — including a derived one, after
    /// which the extent is consumed in full whether the projection reads all of it or not.
    /// </summary>
    public static IShape<T> Sized<T>(this IShape<T> shape, IAreaStrategy area)
      => NotNull(shape).WithPlacement(shape.Placement.WithArea(area));

    /// <summary>
    /// Insets the shape's extent by <paramref name="all"/> cells on every side.
    /// </summary>
    public static IShape<T> Padded<T>(this IShape<T> shape, int all)
    {
      NotNegative(all, nameof(all));

      return Pad(shape, all, all, all, all);
    }

    /// <summary>
    /// Insets the shape's extent by <paramref name="horizontal"/> cells left and right and
    /// <paramref name="vertical"/> cells top and bottom.
    /// </summary>
    public static IShape<T> Padded<T>(this IShape<T> shape, int horizontal, int vertical)
    {
      NotNegative(horizontal, nameof(horizontal));
      NotNegative(vertical, nameof(vertical));

      return Pad(shape, horizontal, vertical, horizontal, vertical);
    }

    /// <summary>
    /// Insets the shape's extent by the given amounts, so the shape reads the inside of its region
    /// and still consumes the whole of it — a border of labels around a block of numbers, say.
    /// <para>
    /// Padding shrinks the inside, where an offset shifts the outside; that is the difference
    /// between this and the movement modifiers, and the two compose freely.
    /// </para>
    /// </summary>
    public static IShape<T> Padded<T>(this IShape<T> shape, int left, int top, int right, int bottom)
    {
      NotNegative(left, nameof(left));
      NotNegative(top, nameof(top));
      NotNegative(right, nameof(right));
      NotNegative(bottom, nameof(bottom));

      return Pad(shape, left, top, right, bottom);
    }

    /// <summary>
    /// Projects the shape's result through <paramref name="selector"/>. The wrapper is a shape like
    /// any other, so <c>Named</c> and the placement modifiers work on either side of it.
    /// </summary>
    public static IShape<TResult> Select<T, TResult>(this IShape<T> shape, Func<T, TResult> selector)
      => new MapShape<T, TResult>(NotNull(shape), selector, Placement.Default);

    /// <summary>
    /// Carries the shape on from wherever it already sits, so movements read cumulatively. A shape
    /// that has not been placed yet has nothing to carry on from and simply takes the new offset.
    /// </summary>
    private static IShape<T> Move<T>(IShape<T> shape, IOffsetStrategy offset)
    {
      var placement = NotNull(shape).Placement;

      return shape.WithPlacement(placement.WithOffset(
        placement.HasDeclaredOffset ? OffsetStrategies.Then(placement.Offset, offset) : offset));
    }

    private static IShape<T> Pad<T>(IShape<T> shape, int left, int top, int right, int bottom)
      => new PadShape<T>(NotNull(shape), left, top, right, bottom, Placement.Default);

    private static IShape<T> NotNull<T>(IShape<T> shape) => shape ?? throw new ArgumentNullException(nameof(shape));

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A shape cannot be inset or moved a negative distance.");
  }
}
