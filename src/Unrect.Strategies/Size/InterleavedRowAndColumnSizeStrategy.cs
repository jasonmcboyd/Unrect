using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Rows over the full available width, then columns within the rows they found — the
  /// <see cref="RowAndColumnSizeStrategy"/> reading of that, taken as ONE forward walk instead of two
  /// passes, so the height stays discoverable a row at a time after the width has been decided.
  /// <para>
  /// The width has to be settled before the extent is handed out and it depends on the row bound,
  /// which looks like a contradiction and is not: both halves consume the same rows in the same
  /// order. The walk asks the row rule about row 0, 1, 2, … and feeds each row it accepts to the
  /// column accumulator, stopping as soon as the accumulator can no longer change — one row, where
  /// the data is dense. The rows it consumed are therefore exactly the rows the height would have
  /// consumed first: nothing is read twice, and nothing is read early. Where the data is sparse
  /// enough that the columns need the whole band, the width decision forces the whole bound, which
  /// is honest rather than lazy.
  /// </para>
  /// <para>
  /// Settling early gives the answer the whole band would, because each accumulator's answer is
  /// monotone in the rows it has seen and "settled" is that monotone sequence's fixed point. An "any"
  /// rule's leading run of matched columns only ever grows, so once it spans the full width no
  /// further row can move it; an "all" rule's leading run only ever shrinks, so once it reaches zero
  /// no further row can move it either. In both cases the answer after <c>k</c> accepted rows is the
  /// answer after all of them — which is what <c>GetSize</c>, defined as the fold of this scan, has
  /// to keep true against the two-pass reading.
  /// </para>
  /// </summary>
  internal sealed class InterleavedRowAndColumnSizeStrategy : IIncrementalSizeStrategy
  {
    public InterleavedRowAndColumnSizeStrategy(
      IIncrementalRowStrategy rowSelectionStrategy,
      IRowMajorColumnStrategy columnSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
    }

    private IIncrementalRowStrategy RowSelectionStrategy { get; }
    private IRowMajorColumnStrategy ColumnSelectionStrategy { get; }

    public IAreaScan BeginSize(ISpace availableSpace)
      => new Scan(
        availableSpace,
        RowSelectionStrategy.BeginRows(),
        ColumnSelectionStrategy.BeginColumns(availableSpace.Area.Width));

    public Size GetSize(ISpace availableSpace) => Scans.FoldSize(BeginSize(availableSpace), availableSpace);

    private sealed class Scan : IAreaScan
    {
      /// <summary>
      /// How many rows the width phase took a verdict on and accepted. They are rows 0 to
      /// <c>_accepted - 1</c>, contiguously, because the first refusal ends the walk.
      /// </summary>
      private int _accepted;

      /// <summary>
      /// Whether the width phase read the row that ends the extent — the row at <see cref="_accepted"/>,
      /// whose refusal is remembered here rather than asked for again.
      /// </summary>
      private bool _stopped;

      public Scan(ISpace space, IRowScan rows, IColumnAccumulator columns)
      {
        Rows = rows;
        Width = DecideWidth(space, rows, columns);
      }

      /// <inheritdoc/>
      public int Width { get; }

      private IRowScan Rows { get; }

      /// <inheritdoc/>
      public bool IncludesRow(ISpace space, int row)
      {
        // Replayed, not re-read. A row rule is told each row once — some carry state that says so —
        // and the width phase already told this one about every row up to and including the one that
        // stopped it. Replaying from what that phase recorded is what keeps the cell reads single-pass.
        if (row < _accepted)
          return true;

        if (_stopped)
          return false;

        return Rows.IncludesRow(space, row);
      }

      /// <summary>
      /// The one forward walk: the row rule's verdicts, with every accepted row fed to the column
      /// accumulator, for as long as the accumulator could still change its mind.
      /// </summary>
      private int DecideWidth(ISpace space, IRowScan rows, IColumnAccumulator columns)
      {
        var height = space.Area.Height;

        while (!columns.IsSettled && _accepted < height)
        {
          if (!rows.IncludesRow(space, _accepted))
          {
            _stopped = true;
            break;
          }

          columns.Include(space, _accepted);
          _accepted++;
        }

        return columns.Count;
      }
    }
  }
}
