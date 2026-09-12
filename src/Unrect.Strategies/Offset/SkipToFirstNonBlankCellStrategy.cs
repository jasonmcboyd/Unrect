using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The lazy corner heuristic: the first non-blank cell scanning row-major from the top-left —
  /// down to the first row that carries content, then across that row to its first non-blank cell.
  /// The offset is that cell's (column, row).
  /// <para>
  /// It reads a row at a time and stops at the first with content, reading across each row only as
  /// far as it takes to decide "any content?" — never scanning down a column, so the height axis is
  /// touched no further than the first content row. That is the lazy / column-cheap profile: rows
  /// touched is the leading blank rows plus the one content row.
  /// </para>
  /// <para>
  /// It finds the first content row's first non-blank cell, which is the region's true corner only
  /// when the region is top-left-aligned. A ragged region whose lower rows reach further left than
  /// its first content row starts at the wrong column and loses the left part — the documented,
  /// accepted miss (see docs/design/table-extent-and-blank-rows.md; the eager escape hatch is where
  /// that region is meant to go). An entirely blank space resolves to its end, so the resulting
  /// subspace is empty, exactly as <see cref="OffsetStrategies.SkipBlankRows"/> does.
  /// </para>
  /// </summary>
  internal sealed class SkipToFirstNonBlankCellStrategy : IOffsetStrategy
  {
    public Offset GetOffset(ISpace space)
    {
      var area = space.Area;

      for (int row = 0; row < area.Height; row++)
        for (int column = 0; column < area.Width; column++)
          if (space[column, row].HasValue)
            return new Offset(column, row);

      // No content anywhere: skip past every row, so the subspace this offset opens is empty rather
      // than a throw — the all-blank answer SkipBlankRows gives.
      return new Offset(0, area.Height);
    }
  }
}
