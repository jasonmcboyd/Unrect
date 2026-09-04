using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A sheet as a space, read a window at a time. The same shape as a grid — an offset and an extent
  /// into shared backing data — except that the backing data is a file and only part of it is in
  /// memory at once.
  /// <para>
  /// Slicing is free and slices share the store, so the engine's subspaces do not multiply the
  /// resident set: a declaration that decomposes a sheet into a hundred regions still holds one
  /// window.
  /// </para>
  /// <para>
  /// A view is a value, not a handle. It has no <c>Dispose</c>, no <c>Close</c>: it can be sliced,
  /// passed to any shape and held as long as the caller likes, and the only thing that invalidates
  /// it is the <see cref="Workbook"/> it came from being disposed.
  /// </para>
  /// <para>
  /// The extent is the sheet's own, as the workbook settled it: what the reader reported, or — for
  /// a sheet whose reader would not say — what measuring it found. Either way it is a real extent,
  /// never an upper bound, so running off it is an ordinary
  /// <see cref="OutOfBoundsException"/> as it is for any space.
  /// </para>
  /// </summary>
  internal sealed class WindowedSpace : ISpace
  {
    internal WindowedSpace(SheetStore store)
      : this(store, default, new Area(store.ColumnCount, store.RowCount))
    {
    }

    private WindowedSpace(SheetStore store, Offset offset, Area area)
    {
      Store = store;
      Offset = offset;
      Area = area;
    }

    internal SheetStore Store { get; }

    private Offset Offset { get; }

    /// <inheritdoc/>
    public Area Area { get; }

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        // OutOfBoundsException, not IndexOutOfRangeException: the engine's fault list classifies the
        // latter as a bug in the reading code — non-absorbable, and rightly so — while running off
        // the end of a space is an ordinary bounds condition that a declaration is allowed to
        // recover from. Getting this wrong would make every overrun unrecoverable.
        if (column < 0 || column >= Area.Width)
          throw new OutOfBoundsException();

        if (row < 0 || row >= Area.Height)
          throw new OutOfBoundsException();

        // The extent travels down with the cell. It is the one thing this layer knows and the store
        // does not, and it is exactly what the store needs to tell a sweep of a bounded band apart
        // from a walk down the sheet.
        return Store.GetCell(Offset.Width + column, Offset.Height + row, Offset.Height, Area.Height);
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Area.Width || offset.Height + area.Height > Area.Height)
        throw new OutOfBoundsException();

      return new WindowedSpace(Store, offset + Offset, area);
    }
  }
}
