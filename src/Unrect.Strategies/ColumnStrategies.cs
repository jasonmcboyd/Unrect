using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="ILineStrategy{TSpace}"/> — the column twin of <see cref="RowStrategies"/>.</summary>
  public static class ColumnStrategies
  {
    /// <summary>Leading columns for which <paramref name="predicate"/> holds; stops at the first column it does not, keeping the match out.</summary>
    public static ILineStrategy<TSpace> TakeColumnsWhile<TSpace>(Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => new TakeWhileColumnStrategy<TSpace>(predicate);

    /// <summary>
    /// Columns while <paramref name="predicate"/> holds of the cell in <paramref name="row"/> — the
    /// transpose of <see cref="RowStrategies.TakeRowsWhile{TSpace}(int, Func{Point{TSpace}, int, bool})"/>, for
    /// reading a band off one caption row.
    /// </summary>
    public static ILineStrategy<TSpace> TakeColumnsWhile<TSpace>(int row, Func<Point<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => TakeColumnsWhile<TSpace>((space, column) => predicate(space[column, row], column));

    /// <summary>Exactly <paramref name="count"/> columns; throws <see cref="OutOfBoundsException"/> when that does not fit.</summary>
    public static ILineStrategy<TSpace> TakeColumns<TSpace>(int count)
      where TSpace : class, ISpace
      => new ExplicitColumnCountStrategy<TSpace>(count);

    /// <summary>
    /// Columns up to and including the first satisfying <paramref name="predicate"/> — the transpose
    /// of <see cref="RowStrategies.TakeRowsTo"/>. The match is kept, where a while-strategy stops
    /// before it.
    /// </summary>
    public static ILineStrategy<TSpace> TakeColumnsTo<TSpace>(Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => new TakeToColumnStrategy<TSpace>(predicate);

    /// <summary>
    /// Every column of the available space. The declared spelling of "the full width", which
    /// otherwise has to be written as the opaque constant predicate <c>(s, c) =&gt; true</c>.
    /// </summary>
    public static ILineStrategy<TSpace> AllColumns<TSpace>()
      where TSpace : class, ISpace
      => TakeColumnsWhile<TSpace>((_, _) => true);

    /// <summary>Leading columns in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static ILineStrategy<TSpace> TakeColumnsWhileAll<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new TakeWhileAllColumnStrategy<TSpace>(predicate);

    /// <summary>Leading columns in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static ILineStrategy<TSpace> TakeColumnsWhileAny<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new TakeWhileAnyColumnStrategy<TSpace>(predicate);

    /// <summary>Leading columns that carry a value — <see cref="TakeColumnsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static ILineStrategy<TSpace> TakeColumnsWhileAnyIsNotBlank<TSpace>()
      where TSpace : class, ISpace
      => TakeColumnsWhileAny<TSpace>(v => !v.IsBlank());

    /// <summary>
    /// A table's columns: any leading columns the first row leaves blank — columns with no caption,
    /// which nothing binds to — and then the columns that carry a value.
    /// </summary>
    internal static ILineStrategy<TSpace> TakeTableColumns<TSpace>()
      where TSpace : class, ISpace
      => new TakeWhileAnyColumnStrategy<TSpace>(v => !v.IsBlank(), afterLead: true);

    /// <summary>Combines <paramref name="strategy"/>'s rows with columns selected by <see cref="TakeColumnsWhile{TSpace}(Func{Plane{TSpace}, int, bool})"/>, rows measured first.</summary>
    public static ISizeStrategy<TSpace> TakeColumnsWhile<TSpace>(
      this ILineStrategy<TSpace> strategy,
      Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => SizeStrategies.RowsThenColumns(strategy, TakeColumnsWhile(predicate));

    /// <summary>Combines <paramref name="strategy"/>'s rows with columns selected by <see cref="TakeColumnsWhileAll{TSpace}(Func{Point{TSpace}, bool})"/>, rows measured first.</summary>
    public static ISizeStrategy<TSpace> TakeColumnsWhileAll<TSpace>(
      this ILineStrategy<TSpace> strategy,
      Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => SizeStrategies.RowsThenColumns(strategy, TakeColumnsWhileAll(predicate));

    /// <summary>Combines <paramref name="strategy"/>'s rows with columns selected by <see cref="TakeColumnsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/>, rows measured first.</summary>
    public static ISizeStrategy<TSpace> TakeColumnsWhileAny<TSpace>(
      this ILineStrategy<TSpace> strategy,
      Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => SizeStrategies.RowsThenColumns(strategy, TakeColumnsWhileAny(predicate));

    /// <summary>Those rows, at the columns that carry values — <see cref="TakeColumnsWhileAny{TSpace}(Func{Point{TSpace}, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static ISizeStrategy<TSpace> TakeColumnsWhileAnyIsNotBlank<TSpace>(this ILineStrategy<TSpace> strategy)
      where TSpace : class, ISpace
      => strategy.TakeColumnsWhileAny(v => !v.IsBlank());

    /// <summary>Those rows, at the full available width.</summary>
    public static ISizeStrategy<TSpace> AllColumns<TSpace>(this ILineStrategy<TSpace> strategy)
      where TSpace : class, ISpace
      => strategy.TakeColumnsWhile((_, _) => true);
  }
}
