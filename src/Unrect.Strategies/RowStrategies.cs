using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="ILineStrategy{TSpace}"/> — how many of a space's leading rows a projection claims.</summary>
  public static class RowStrategies
  {
    /// <summary>Leading rows for which <paramref name="predicate"/> holds; stops at the first row it does not, keeping the match out.</summary>
    public static ILineStrategy<TSpace> TakeRowsWhile<TSpace>(Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => new TakeToRowStrategy<TSpace>(predicate.Not(), false);

    /// <summary>Leading rows while <paramref name="predicate"/> holds of the cell in <paramref name="column"/> — for reading a band off one label column.</summary>
    public static ILineStrategy<TSpace> TakeRowsWhile<TSpace>(int column, Func<Point<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => TakeRowsWhile<TSpace>((space, row) => predicate(space[column, row], row));

    /// <summary>Exactly <paramref name="count"/> rows; throws <see cref="OutOfBoundsException"/> when that does not fit.</summary>
    public static ILineStrategy<TSpace> TakeRows<TSpace>(int count)
      where TSpace : class, ISpace
      => new ExplicitRowCountStrategy<TSpace>(count);

    /// <summary>Rows up to and including the first for which <paramref name="predicate"/> holds — the match is kept, where <see cref="TakeRowsWhile{TSpace}(Func{Plane{TSpace}, int, bool})"/> stops before it.</summary>
    public static ILineStrategy<TSpace> TakeRowsTo<TSpace>(Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => new TakeToRowStrategy<TSpace>(predicate, true);

    /// <summary>Leading rows in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static ILineStrategy<TSpace> TakeRowsWhileAll<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new TakeWhileAllRowStrategy<TSpace>(predicate);

    /// <summary>Leading rows in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static ILineStrategy<TSpace> TakeRowsWhileAny<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new TakeWhileAnyRowStrategy<TSpace>(predicate);

    /// <summary>Leading rows that carry a value — <see cref="TakeRowsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static ILineStrategy<TSpace> TakeRowsWhileAnyIsNotBlank<TSpace>()
      where TSpace : class, ISpace
      => TakeRowsWhileAny<TSpace>(v => !v.IsBlank());

    /// <summary>
    /// Every row of the available space. The declared spelling of "the full height", which
    /// otherwise has to be written as the opaque constant predicate <c>(s, r) =&gt; true</c>.
    /// </summary>
    public static ILineStrategy<TSpace> AllRows<TSpace>()
      where TSpace : class, ISpace
      => TakeRowsWhile<TSpace>((_, _) => true);

    /// <summary>Combines <paramref name="strategy"/>'s columns with rows selected by <see cref="TakeRowsWhile{TSpace}(Func{Plane{TSpace}, int, bool})"/>, columns measured first.</summary>
    public static ISizeStrategy<TSpace> TakeRowsWhile<TSpace>(
      this ILineStrategy<TSpace> strategy,
      Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => SizeStrategies.ColumnsThenRows(strategy, TakeRowsWhile(predicate));

    /// <summary>Combines <paramref name="strategy"/>'s columns with rows selected by <see cref="TakeRowsWhileAll{TSpace}(Func{Point{TSpace}, bool})"/>, columns measured first.</summary>
    public static ISizeStrategy<TSpace> TakeRowsWhileAll<TSpace>(
      this ILineStrategy<TSpace> strategy,
      Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => SizeStrategies.ColumnsThenRows(strategy, TakeRowsWhileAll(predicate));

    /// <summary>Combines <paramref name="strategy"/>'s columns with rows selected by <see cref="TakeRowsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/>, columns measured first.</summary>
    public static ISizeStrategy<TSpace> TakeRowsWhileAny<TSpace>(
      this ILineStrategy<TSpace> strategy,
      Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => SizeStrategies.ColumnsThenRows(strategy, TakeRowsWhileAny(predicate));

    /// <summary>Those columns, at the rows that carry values — <see cref="TakeRowsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static ISizeStrategy<TSpace> TakeRowsWhileAnyIsNotBlank<TSpace>(this ILineStrategy<TSpace> strategy)
      where TSpace : class, ISpace
      => strategy.TakeRowsWhileAny(v => !v.IsBlank());

    /// <summary>Those columns, at the full available height.</summary>
    public static ISizeStrategy<TSpace> AllRows<TSpace>(this ILineStrategy<TSpace> strategy)
      where TSpace : class, ISpace
      => strategy.TakeRowsWhile((_, _) => true);
  }
}
