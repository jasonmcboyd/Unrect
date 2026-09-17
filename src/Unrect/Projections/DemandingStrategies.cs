using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A size rule that can only look at a space offering at least <typeparamref name="TSpace"/> —
  /// one whose predicate asks about a cell's kind or its value rather than the four questions every
  /// space answers.
  /// <para>
  /// Like <see cref="IRowLandmark{TSpace}"/> it deliberately does <em>not</em> derive from
  /// <see cref="ISizeStrategy"/>: keeping the two families apart is what makes the demand visible
  /// to the member that takes one, and what lets a declaration carry it with nothing annotated. The
  /// strategy calculus, which knows nothing of capabilities, still runs it — the lift unwraps
  /// <see cref="Strategy"/> and hands the calculus the rule itself.
  /// </para>
  /// <para>
  /// <c>in TSpace</c>, so a rule built over a less demanding space composes into a declaration
  /// written over a more demanding one.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space this rule must be able to look at.</typeparam>
  public interface ISizeStrategy<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The rule as the strategy calculus takes it, its demand discharged by the lift.</summary>
    ISizeStrategy Strategy { get; }
  }

  /// <summary>The offset twin of <see cref="ISizeStrategy{TSpace}"/>.</summary>
  /// <typeparam name="TSpace">The space this rule must be able to look at.</typeparam>
  public interface IOffsetStrategy<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The rule as the strategy calculus takes it, its demand discharged by the lift.</summary>
    IOffsetStrategy Strategy { get; }
  }

  /// <summary>The area twin of <see cref="ISizeStrategy{TSpace}"/>.</summary>
  /// <typeparam name="TSpace">The space this rule must be able to look at.</typeparam>
  public interface IAreaStrategy<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The rule as the strategy calculus takes it, its demand discharged by the lift.</summary>
    IAreaStrategy Strategy { get; }
  }

  /// <summary>The row-count twin of <see cref="ISizeStrategy{TSpace}"/>.</summary>
  /// <typeparam name="TSpace">The space this rule must be able to look at.</typeparam>
  public interface IRowStrategy<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The rule as the strategy calculus takes it, its demand discharged by the lift.</summary>
    IRowStrategy Strategy { get; }
  }

  /// <summary>The column-count twin of <see cref="ISizeStrategy{TSpace}"/>.</summary>
  /// <typeparam name="TSpace">The space this rule must be able to look at.</typeparam>
  public interface IColumnStrategy<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The rule as the strategy calculus takes it, its demand discharged by the lift.</summary>
    IColumnStrategy Strategy { get; }
  }

  /// <summary>
  /// A canonical strategy or matcher wearing a demand — what a typed factory returns once it has
  /// lowered its predicate and called the erased factory. The box holds the rule and nothing else,
  /// so the object the calculus receives is the rule itself rather than a wrapper around it.
  /// </summary>
  internal static partial class Demanding
  {
    internal static ISizeStrategy<TSpace> Size<TSpace>(ISizeStrategy strategy)
      where TSpace : class, ISpace
      => new DemandedSize<TSpace>(strategy);

    internal static IOffsetStrategy<TSpace> Offset<TSpace>(IOffsetStrategy strategy)
      where TSpace : class, ISpace
      => new DemandedOffset<TSpace>(strategy);

    internal static IAreaStrategy<TSpace> Area<TSpace>(IAreaStrategy strategy)
      where TSpace : class, ISpace
      => new DemandedArea<TSpace>(strategy);

    internal static IRowStrategy<TSpace> Rows<TSpace>(IRowStrategy strategy)
      where TSpace : class, ISpace
      => new DemandedRows<TSpace>(strategy);

    internal static IColumnStrategy<TSpace> Columns<TSpace>(IColumnStrategy strategy)
      where TSpace : class, ISpace
      => new DemandedColumns<TSpace>(strategy);

    private sealed class DemandedSize<TSpace> : ISizeStrategy<TSpace>
      where TSpace : class, ISpace
    {
      internal DemandedSize(ISizeStrategy strategy) => Strategy = strategy;

      public ISizeStrategy Strategy { get; }
    }

    private sealed class DemandedOffset<TSpace> : IOffsetStrategy<TSpace>
      where TSpace : class, ISpace
    {
      internal DemandedOffset(IOffsetStrategy strategy) => Strategy = strategy;

      public IOffsetStrategy Strategy { get; }
    }

    private sealed class DemandedArea<TSpace> : IAreaStrategy<TSpace>
      where TSpace : class, ISpace
    {
      internal DemandedArea(IAreaStrategy strategy) => Strategy = strategy;

      public IAreaStrategy Strategy { get; }
    }

    private sealed class DemandedRows<TSpace> : IRowStrategy<TSpace>
      where TSpace : class, ISpace
    {
      internal DemandedRows(IRowStrategy strategy) => Strategy = strategy;

      public IRowStrategy Strategy { get; }
    }

    private sealed class DemandedColumns<TSpace> : IColumnStrategy<TSpace>
      where TSpace : class, ISpace
    {
      internal DemandedColumns(IColumnStrategy strategy) => Strategy = strategy;

      public IColumnStrategy Strategy { get; }
    }
  }
}
