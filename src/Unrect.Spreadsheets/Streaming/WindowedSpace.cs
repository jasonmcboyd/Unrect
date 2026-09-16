using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A whole sheet as a space, read a window at a time: the extent is the sheet's, the coordinates
  /// are the sheet's, and only part of it is in memory at once.
  /// <para>
  /// A region of it is arithmetic done by a plane, so a declaration that decomposes a sheet into a
  /// hundred regions allocates nothing, makes no second space, and still holds one window.
  /// </para>
  /// <para>
  /// Which band is open is the one thing this layer cannot work out for itself, because a region is
  /// arithmetic over the sheet rather than an object wrapping part of it. The engine says so
  /// instead, once per placement, through <see cref="ISweepAware"/>.
  /// </para>
  /// <para>
  /// A view is a value, not a handle. It has no <c>Dispose</c>, no <c>Close</c>: it can be sliced,
  /// passed to any projection and held as long as the caller likes, and the only thing that
  /// invalidates it is the <see cref="Workbook"/> it came from being disposed.
  /// </para>
  /// <para>
  /// The extent is the sheet's own, as the workbook settled it: what the reader reported, or — for
  /// a sheet whose reader would not say — what measuring it found. Either way it is a real extent,
  /// never an upper bound, so running off it is an ordinary
  /// <see cref="OutOfBoundsException"/> as it is for any space.
  /// </para>
  /// </summary>
  internal sealed class WindowedSpace : SheetCellsBase, ISweepAware
  {
    internal WindowedSpace(SheetStore store)
    {
      Store = store;
      Area = new Area(store.ColumnCount, store.RowCount);
    }

    internal SheetStore Store { get; }

    /// <inheritdoc/>
    public override Area Area { get; }

    private protected override Cell CellAt(int column, int row)
    {
      // OutOfBoundsException, not IndexOutOfRangeException: the engine's fault list classifies the
      // latter as a bug in the reading code — non-absorbable, and rightly so — while running off
      // the end of a space is an ordinary bounds condition that a declaration is allowed to recover
      // from. Getting this wrong would make every overrun unrecoverable.
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return Store.GetCell(column, row);
    }

    /// <summary>
    /// The band a placement has opened, passed to the store so its window keeps those rows while
    /// the band is being swept. The origin is a sheet row already, because this space is the whole
    /// sheet and a plane's coordinates are root.
    /// </summary>
    /// <param name="origin">Where the region starts.</param>
    /// <param name="area">How big the region was declared.</param>
    public void Sweeping(Offset origin, Area area) => Store.Sweeping(origin.Height, area.Height);
  }
}
