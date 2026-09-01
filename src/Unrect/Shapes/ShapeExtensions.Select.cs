using System;

namespace Unrect.Shapes
{
  /// <summary>
  /// Mechanical: <c>Select</c> over the tuple results of stacks, arities 2 through 8, written out
  /// rather than abstracted. Each one just unpacks the tuple into a multi-argument selector, so a
  /// stack's children arrive as named parameters instead of <c>Item1</c>, <c>Item2</c>, ....
  /// </summary>
  public static partial class ShapeExtensions
  {
    /// <summary>Projects the 2 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, TResult>(
      this IShape<(T1, T2)> shape,
      Func<T1, T2, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2));

    /// <summary>Projects the 3 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, T3, TResult>(
      this IShape<(T1, T2, T3)> shape,
      Func<T1, T2, T3, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2, value.Item3));

    /// <summary>Projects the 4 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, T3, T4, TResult>(
      this IShape<(T1, T2, T3, T4)> shape,
      Func<T1, T2, T3, T4, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2, value.Item3, value.Item4));

    /// <summary>Projects the 5 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, T3, T4, T5, TResult>(
      this IShape<(T1, T2, T3, T4, T5)> shape,
      Func<T1, T2, T3, T4, T5, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5));

    /// <summary>Projects the 6 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, T3, T4, T5, T6, TResult>(
      this IShape<(T1, T2, T3, T4, T5, T6)> shape,
      Func<T1, T2, T3, T4, T5, T6, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6));

    /// <summary>Projects the 7 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, T3, T4, T5, T6, T7, TResult>(
      this IShape<(T1, T2, T3, T4, T5, T6, T7)> shape,
      Func<T1, T2, T3, T4, T5, T6, T7, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7));

    /// <summary>Projects the 8 results of a stack through <paramref name="selector"/>.</summary>
    public static IShape<TResult> Select<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
      this IShape<(T1, T2, T3, T4, T5, T6, T7, T8)> shape,
      Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> selector)
      => shape.Select(value => selector(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Item8));
  }
}
