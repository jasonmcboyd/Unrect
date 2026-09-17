using System;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// How a predicate written over a named space reaches the strategy calculus, which asks only the
  /// four questions every space answers and therefore speaks <see cref="Plane{TSpace}"/> and
  /// <see cref="Point{TSpace}"/> over <see cref="ISpace"/>.
  /// <para>
  /// The cast is per evaluation rather than once per call: a strategy that cast when it was measured
  /// would have to be a wrapper around the calculus's own, and a wrapper is what would drop the
  /// incremental interfaces the engine type-tests for. A failed cast is a fault — the phantom's
  /// static type is what confines a typed rule to a declaration closed over its space.
  /// </para>
  /// <para>
  /// <b>A null predicate is refused here</b>, and this is the only place it can be: a lowered null
  /// is a live delegate, so the factory below would build a declaration that fails when it runs
  /// rather than when it is written. The name in the failure is the text of the argument at the call
  /// site, which is the parameter the caller wrote.
  /// </para>
  /// </summary>
  internal static class TypedPredicates
  {
    /// <summary>A cell predicate, as the calculus takes it.</summary>
    internal static Func<Point<ISpace>, bool> Lower<TSpace>(
      Func<Point<TSpace>, bool> cell,
      [CallerArgumentExpression("cell")] string? named = null)
      where TSpace : class, ISpace
    {
      Required(cell, named);

      return point => cell(point.Retyped<TSpace>());
    }

    /// <summary>A predicate over a cell and the row or column it sits in, as the calculus takes it.</summary>
    internal static Func<Point<ISpace>, int, bool> Lower<TSpace>(
      Func<Point<TSpace>, int, bool> cell,
      [CallerArgumentExpression("cell")] string? named = null)
      where TSpace : class, ISpace
    {
      Required(cell, named);

      return (point, index) => cell(point.Retyped<TSpace>(), index);
    }

    /// <summary>A predicate over a region and one of its rows or columns, as the calculus takes it.</summary>
    internal static Func<Plane<ISpace>, int, bool> Lower<TSpace>(
      Func<Plane<TSpace>, int, bool> line,
      [CallerArgumentExpression("line")] string? named = null)
      where TSpace : class, ISpace
    {
      Required(line, named);

      return (plane, index) => line(plane.Retyped<TSpace>(), index);
    }

    /// <summary>A region measured by hand, as the calculus takes it.</summary>
    internal static Func<Plane<ISpace>, Size> Lower<TSpace>(
      Func<Plane<TSpace>, Size> measure,
      [CallerArgumentExpression("measure")] string? named = null)
      where TSpace : class, ISpace
    {
      Required(measure, named);

      return plane => measure(plane.Retyped<TSpace>());
    }

    private static void Required(Delegate? rule, string? named)
    {
      if (rule is null)
        throw new ArgumentNullException(named);
    }
  }
}
