using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A column rule read one row at a time: the answer the rows seen so far give, whether any further
  /// row could change it, and the step that takes one more row into account.
  /// <para>
  /// This is the row-major rewrite of §11.3 exposed rather than kept private, because the width and
  /// height of a rows-then-columns extent are decided by one forward walk over the same rows — see
  /// <see cref="InterleavedRowAndColumnSizeStrategy"/>, which drives an accumulator of its own
  /// alongside a row scan. It is deliberately NOT the column twin of <see cref="IRowScan"/>: a column
  /// rule cannot be discovered as a projection consumes, because a width must be settled before the
  /// first row is handed out. What it can be is accumulated, and settle early.
  /// </para>
  /// </summary>
  internal interface IColumnAccumulator
  {
    /// <summary>How many columns the rows taken into account so far select.</summary>
    int Count { get; }

    /// <summary>
    /// Whether <see cref="Count"/> is final — no further row could change it. Each rule's own fixed
    /// point: a rule whose answer only grows is settled at the full width, one whose answer only
    /// shrinks is settled at zero.
    /// </summary>
    bool IsSettled { get; }

    /// <summary>Takes row <paramref name="row"/> of <paramref name="space"/> into account.</summary>
    void Include(ISpace space, int row);
  }
}
