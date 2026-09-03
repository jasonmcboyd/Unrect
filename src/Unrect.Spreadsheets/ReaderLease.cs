using System.Threading.Tasks;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// One slot in the <see cref="ReaderPool"/>: a cursor, where it stands, and how far it has
  /// travelled to get there.
  /// <para>
  /// A slot exists whether or not it holds a cursor — <c>MaxReaders</c> slots are created up front
  /// and filled as demand proves them worth their ~5s open. The position is the pair
  /// <c>(SheetIndex, CursorRow)</c>, ordered lexicographically, because a reader can move to a later
  /// sheet but never an earlier one: a workbook position is not a row number.
  /// </para>
  /// <para>
  /// Every field here is read and written under the pool's gate, except while the lease is checked
  /// out — at which point exactly one thread owns it, and the streaming of rows happens with the
  /// gate released.
  /// </para>
  /// </summary>
  internal sealed class ReaderLease
  {
    internal ReaderLease(int index)
    {
      Index = index;
    }

    /// <summary>The slot number, which is also this lease's place in <c>RowsPerReader</c>.</summary>
    internal int Index { get; }

    /// <summary>The open cursor, or null while this slot is unfilled.</summary>
    internal IRowCursor? Cursor { get; set; }

    /// <summary>The sheet the cursor stands on.</summary>
    internal int SheetIndex { get; set; }

    /// <summary>
    /// The index of the row the next <see cref="IRowCursor.Read"/> will return: zero on a freshly
    /// opened or freshly advanced sheet, and one more after every row consumed.
    /// </summary>
    internal int CursorRow { get; set; }

    /// <summary>Rows this lease has moved over in its life, skipped and read alike.</summary>
    internal long RowsAdvanced { get; set; }

    /// <summary>
    /// True when a warmer opened this cursor and nobody has used it yet. It is consumed by the first
    /// borrow, which is the moment a <c>WarmHit</c> is counted — the open was already paid for.
    /// </summary>
    internal bool IsWarm { get; set; }

    /// <summary>True while a borrower holds this lease.</summary>
    internal bool InUse { get; set; }

    /// <summary>The warm in flight on this slot, if any. Null once it has completed.</summary>
    internal Task? Warming { get; set; }

    /// <summary>Whether this slot currently holds a cursor.</summary>
    internal bool IsOpen => Cursor is not null;

    /// <summary>
    /// Whether this lease stands at or behind <paramref name="sheetIndex"/>, <paramref name="row"/>
    /// — that is, whether it can reach the target by moving forward.
    /// </summary>
    internal bool IsAtOrBehind(int sheetIndex, int row) =>
      SheetIndex < sheetIndex || (SheetIndex == sheetIndex && CursorRow <= row);

    /// <summary>
    /// Whether this lease stands strictly further along than <paramref name="other"/>. The
    /// comparison a "furthest along but still behind the target" search is built from.
    /// </summary>
    internal bool IsAheadOf(ReaderLease other) =>
      SheetIndex > other.SheetIndex || (SheetIndex == other.SheetIndex && CursorRow > other.CursorRow);

    /// <summary>
    /// Whether this lease stands strictly past <paramref name="sheetIndex"/>, <paramref name="row"/>
    /// — the condition that makes a reach <em>backward</em>.
    /// </summary>
    internal bool IsPast(int sheetIndex, int row) =>
      SheetIndex > sheetIndex || (SheetIndex == sheetIndex && CursorRow > row);

    /// <summary>
    /// Moves the cursor forward to <paramref name="sheetIndex"/>, <paramref name="row"/>, returning
    /// the rows parsed and discarded on the way. Called with the lease checked out and the pool gate
    /// released, because this is where the reading happens.
    /// </summary>
    internal long AdvanceTo(int sheetIndex, int row)
    {
      var cursor = Cursor!;
      var skipped = 0L;

      while (SheetIndex < sheetIndex && cursor.NextSheet())
      {
        SheetIndex = cursor.SheetIndex;
        CursorRow = 0;
      }

      while (CursorRow < row && cursor.Read())
      {
        CursorRow++;
        skipped++;
      }

      RowsAdvanced += skipped;

      return skipped;
    }

    /// <summary>
    /// Steps this lease to the next sheet, keeping its position honest. Used by the catalogue walk,
    /// which discovers sheets one at a time rather than aiming at a known index.
    /// </summary>
    internal bool NextSheet()
    {
      if (!Cursor!.NextSheet())
        return false;

      SheetIndex = Cursor.SheetIndex;
      CursorRow = 0;

      return true;
    }

    /// <summary>
    /// Records that one row has been consumed by a load. The store reads rows directly from the
    /// cursor, so the lease has to be told where it now stands.
    /// </summary>
    internal void CountRow()
    {
      CursorRow++;
      RowsAdvanced++;
    }

    /// <summary>Closes the cursor and empties the slot, leaving the travel counter intact.</summary>
    internal void Close()
    {
      Cursor?.Dispose();
      Cursor = null;
      IsWarm = false;
      SheetIndex = 0;
      CursorRow = 0;
    }
  }
}
