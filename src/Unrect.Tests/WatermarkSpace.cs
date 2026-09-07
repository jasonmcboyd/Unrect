using Unrect.Core;

namespace Unrect.Tests
{
  /// <summary>
  /// A space that remembers how far down the sheet the reading has got, and how far back behind that
  /// it ever reached. A windowed reader is only as cheap as the walk over it is monotone, so this is
  /// the instrument that says whether a declaration would survive one — with no streaming machinery
  /// involved, and therefore nothing to argue with.
  /// <para>
  /// It is deliberately coarser than <see cref="CountingSpace"/> and answers a different question.
  /// That one counts what was read (how many cells, how many distinct rows); this one records the
  /// <em>order</em>, which is the only thing a window cares about: a declaration that reads every row
  /// once but reads them out of order costs a reload, and no count can tell you that.
  /// </para>
  /// <para>
  /// Subspaces carry the row translation, so every reach is reported in the outermost space's own
  /// coordinates. Mirrored from the typed-spaces spike, whose scenario 9 first measured this.
  /// </para>
  /// </summary>
  internal sealed class WatermarkSpace : ISpace
  {
    private readonly ISpace _inner;
    private readonly int _firstRow;
    private readonly Trace _trace;

    public WatermarkSpace(ISpace inner)
      : this(inner, 0, new Trace())
    {
    }

    private WatermarkSpace(ISpace inner, int firstRow, Trace trace)
    {
      _inner = inner;
      _firstRow = firstRow;
      _trace = trace;
    }

    /// <summary>The deepest a read ever fell behind the furthest row read so far, in rows.</summary>
    public int BackwardReach => _trace.BackwardReach;

    /// <summary>The furthest row read, in the outermost space's coordinates.</summary>
    public int HighWaterMark => _trace.HighWaterMark;

    /// <inheritdoc/>
    public Area Area => _inner.Area;

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        _trace.Touch(_firstRow + row);

        return _inner[column, row];
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
      => new WatermarkSpace(_inner.GetSubspace(offset, area), _firstRow + offset.Height, _trace);

    private sealed class Trace
    {
      public int HighWaterMark { get; private set; } = -1;

      public int BackwardReach { get; private set; }

      public void Touch(int row)
      {
        if (row > HighWaterMark)
          HighWaterMark = row;
        else if (HighWaterMark - row > BackwardReach)
          BackwardReach = HighWaterMark - row;
      }
    }
  }
}
