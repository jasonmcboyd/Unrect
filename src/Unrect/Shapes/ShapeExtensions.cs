using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// Application, naming, and placement modifiers. Modifiers replace rather than accumulate — the
  /// last one wins; compose offsets explicitly with <see cref="Shape.Then"/>.
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

    /// <summary>Positions the shape at <paramref name="offset"/>, replacing any offset it had.</summary>
    public static IShape<T> After<T>(this IShape<T> shape, IOffsetStrategy offset)
      => NotNull(shape).WithPlacement(shape.Placement.WithOffset(offset));

    /// <summary>Positions the shape past the leading blank rows, replacing any offset it had.</summary>
    public static IShape<T> AfterBlankRows<T>(this IShape<T> shape) => shape.After(OffsetStrategies.SkipBlankRows());

    /// <summary>Positions the shape past the leading blank columns, replacing any offset it had.</summary>
    public static IShape<T> AfterBlankColumns<T>(this IShape<T> shape) => shape.After(OffsetStrategies.SkipBlankColumns());

    /// <summary>
    /// Positions the shape <paramref name="rows"/> rows down, replacing any offset it had — to move
    /// past a discovered offset as well, spell both out with <see cref="Shape.Then"/>.
    /// </summary>
    public static IShape<T> Down<T>(this IShape<T> shape, int rows)
      => shape.After(OffsetStrategies.ExplicitOffset(0, NotNegative(rows, nameof(rows))));

    /// <summary>
    /// Positions the shape <paramref name="columns"/> columns right, replacing any offset it had.
    /// </summary>
    public static IShape<T> Right<T>(this IShape<T> shape, int columns)
      => shape.After(OffsetStrategies.ExplicitOffset(NotNegative(columns, nameof(columns)), 0));

    /// <summary>
    /// Declares the shape's extent, replacing whatever it had — including a derived one, after
    /// which the extent is consumed in full whether the projection reads all of it or not.
    /// </summary>
    public static IShape<T> Sized<T>(this IShape<T> shape, IAreaStrategy area)
      => NotNull(shape).WithPlacement(shape.Placement.WithArea(area));

    /// <summary>
    /// Projects the shape's result through <paramref name="selector"/>. The wrapper is a shape like
    /// any other, so <c>Named</c> and the placement modifiers work on either side of it.
    /// </summary>
    public static IShape<TResult> Select<T, TResult>(this IShape<T> shape, Func<T, TResult> selector)
      => new MapShape<T, TResult>(NotNull(shape), selector, Placement.Default);

    private static IShape<T> NotNull<T>(IShape<T> shape) => shape ?? throw new ArgumentNullException(nameof(shape));

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A shape cannot be moved a negative distance.");
  }
}
