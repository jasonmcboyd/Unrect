using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="IRowStrategy"/> — how many of a space's leading rows a projection claims.</summary>
  public static class RowStrategies
  {
    /// <summary>Leading rows for which <paramref name="predicate"/> holds; stops at the first row it does not, keeping the match out.</summary>
    public static IRowStrategy TakeRowsWhile(Func<Plane<ISpace>, int, bool> predicate)
      => new TakeToRowStrategy(predicate.Not(), false);

    /// <summary>Leading rows while <paramref name="predicate"/> holds of the cell in <paramref name="column"/> — for reading a band off one label column.</summary>
    public static IRowStrategy TakeRowsWhile(int column, Func<Point<ISpace>, int, bool> predicate)
      => TakeRowsWhile((space, row) => predicate(space[column, row], row));

    /// <summary>Exactly <paramref name="count"/> rows; throws <see cref="OutOfBoundsException"/> when that does not fit.</summary>
    public static IRowStrategy TakeRows(int count)
      => new ExplicitRowCountStrategy(count);

    /// <summary>Rows up to and including the first for which <paramref name="predicate"/> holds — the match is kept, where <see cref="TakeRowsWhile(Func{Plane{ISpace}, int, bool})"/> stops before it.</summary>
    public static IRowStrategy TakeRowsTo(Func<Plane<ISpace>, int, bool> predicate)
      => new TakeToRowStrategy(predicate, true);

    /// <summary>Leading rows in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IRowStrategy TakeRowsWhileAll(Func<Point<ISpace>, bool> predicate)
      => new TakeWhileAllRowStrategy(predicate);

    /// <summary>Leading rows in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IRowStrategy TakeRowsWhileAny(Func<Point<ISpace>, bool> predicate)
      => new TakeWhileAnyRowStrategy(predicate);

    /// <summary>Leading rows that carry a value — <see cref="TakeRowsWhileAny(Func{Point{ISpace}, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static IRowStrategy TakeRowsWhileAnyValue()
      => TakeRowsWhileAny(v => v.HasValue());

    /// <summary>
    /// Every row of the available space. The declared spelling of "the full height", which
    /// otherwise has to be written as the opaque constant predicate <c>(s, r) =&gt; true</c>.
    /// </summary>
    public static IRowStrategy AllRows() => TakeRowsWhile((_, _) => true);

    /// <summary>Combines <paramref name="strategy"/>'s columns with rows selected by <see cref="TakeRowsWhile(Func{Plane{ISpace}, int, bool})"/>, columns measured first.</summary>
    public static IAreaStrategy TakeRowsWhile(
      this IColumnStrategy strategy,
      Func<Plane<ISpace>, int, bool> predicate)
      => AreaStrategies.ColumnsThenRows(strategy, TakeRowsWhile(predicate));

    /// <summary>Combines <paramref name="strategy"/>'s columns with rows selected by <see cref="TakeRowsWhileAll(Func{Point{ISpace}, bool})"/>, columns measured first.</summary>
    public static IAreaStrategy TakeRowsWhileAll(
      this IColumnStrategy strategy,
      Func<Point<ISpace>, bool> predicate)
      => AreaStrategies.ColumnsThenRows(strategy, TakeRowsWhileAll(predicate));

    /// <summary>Combines <paramref name="strategy"/>'s columns with rows selected by <see cref="TakeRowsWhileAny(Func{Point{ISpace}, bool})"/>, columns measured first.</summary>
    public static IAreaStrategy TakeRowsWhileAny(
      this IColumnStrategy strategy,
      Func<Point<ISpace>, bool> predicate)
      => AreaStrategies.ColumnsThenRows(strategy, TakeRowsWhileAny(predicate));

    /// <summary>Those columns, at the rows that carry values — <see cref="TakeRowsWhileAny(Func{Point{ISpace}, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static IAreaStrategy TakeRowsWhileAnyValue(this IColumnStrategy strategy)
      => strategy.TakeRowsWhileAny(v => v.HasValue());

    /// <summary>Those columns, at the full available height.</summary>
    public static IAreaStrategy AllRows(this IColumnStrategy strategy)
      => strategy.TakeRowsWhile((_, _) => true);
  }
}
