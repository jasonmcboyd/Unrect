using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  public static class OffsetStrategies
  {
    public static IOffsetStrategy MaxOffset()
      => MaxSize().ToOffsetStrategy();

    public static IOffsetStrategy MinOffset()
      => MinSize().ToOffsetStrategy();

    public static IOffsetStrategy ExplicitOffset(int width, int height)
      => ExplicitSize(width, height).ToOffsetStrategy();

    public static IOffsetStrategy SelectOffset(Func<ISpace, Size> selector)
      => SelectSize(selector).ToOffsetStrategy();

    public static IOffsetStrategy SkipRowsWhileAll(Func<CellValue, bool> predicate)
      => new RowOffsetSizeStrategy(RowStrategies.TakeRowsWhileAll(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipRowsWhileAny(Func<CellValue, bool> predicate)
      => new RowOffsetSizeStrategy(RowStrategies.TakeRowsWhileAny(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipBlankRows()
      => SkipRowsWhileAll(v => v.IsBlank);

    public static IOffsetStrategy SkipColumnsWhileAll(Func<CellValue, bool> predicate)
      => new ColumnOffsetSizeStrategy(ColumnStrategies.TakeColumnsWhileAll(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipColumnsWhileAny(Func<CellValue, bool> predicate)
      => new ColumnOffsetSizeStrategy(ColumnStrategies.TakeColumnsWhileAny(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipBlankColumns()
      => SkipColumnsWhileAll(v => v.IsBlank);

    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets)
      => new CompositeOffsetSizeStrategy(offsets).ToOffsetStrategy();

    // --- Seeking: anchoring on presence ---------------------------------------------------------
    //
    // A skip-while stops at the first row that fails its predicate, so anything inserted above the
    // thing you are looking for moves it. A seek scans to the first row that matches, and the
    // region starts AT that row — "the row after the label" is Then(SeekRow..., SkipRows(1)).
    // Finding nothing is a placement failure: OutOfBoundsException, which a strict shape reports
    // and a repeat treats as "no more sections".

    /// <summary>Down to the first row satisfying <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy SeekRow(Func<ISpace, int, bool> predicate)
      => Seek(new SeekRowStrategy(NotNull(predicate, nameof(predicate)), "no matching row"));

    /// <summary>Down to the first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IOffsetStrategy SeekRowWhere(Func<CellValue, bool> anyCell)
      => Seek(new SeekRowStrategy(AnyCellInRow(NotNull(anyCell, nameof(anyCell))), "no row with a matching cell"));

    /// <summary>
    /// Down to the first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively. Whole-cell equality rather than a substring: labels are cell values, and
    /// substring matching invites false anchors — use <see cref="SeekRowWhere"/> for anything else.
    /// </summary>
    public static IOffsetStrategy SeekRowContaining(string text)
      => Seek(new SeekRowStrategy(
        AnyCellInRow(TextEquals(NotNull(text, nameof(text)))),
        $"no row containing '{text}'"));

    /// <summary>Right to the first column satisfying <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy SeekColumn(Func<ISpace, int, bool> predicate)
      => Seek(new SeekColumnStrategy(NotNull(predicate, nameof(predicate)), "no matching column"));

    /// <summary>Right to the first column with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IOffsetStrategy SeekColumnWhere(Func<CellValue, bool> anyCell)
      => Seek(new SeekColumnStrategy(AnyCellInColumn(NotNull(anyCell, nameof(anyCell))), "no column with a matching cell"));

    /// <summary>
    /// Right to the first column holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively; the column twin of <see cref="SeekRowContaining"/>.
    /// </summary>
    public static IOffsetStrategy SeekColumnContaining(string text)
      => Seek(new SeekColumnStrategy(
        AnyCellInColumn(TextEquals(NotNull(text, nameof(text)))),
        $"no column containing '{text}'"));

    // --- Anchoring to the far edge --------------------------------------------------------------
    //
    // Both measure back from the end of the available space, so they are normally spelled with
    // .After(...), which replaces: composing a movement before a from-end anchor rarely means
    // anything, since the anchor discards where the movement left off.

    /// <summary>The rightmost <paramref name="width"/> columns of the available space.</summary>
    public static IOffsetStrategy FromRight(int width)
    {
      NotNegative(width, nameof(width));

      return SelectOffset(space => new Size(Reserve(space.Area.Size.Width, width), 0));
    }

    /// <summary>The bottom <paramref name="height"/> rows of the available space.</summary>
    public static IOffsetStrategy FromBottom(int height)
    {
      NotNegative(height, nameof(height));

      return SelectOffset(space => new Size(0, Reserve(space.Area.Size.Height, height)));
    }

    /// <summary>How far in to start so that <paramref name="extent"/> reaches the far edge.</summary>
    private static int Reserve(int available, int extent)
      => extent <= available ? available - extent : throw new OutOfBoundsException();

    private static IOffsetStrategy Seek(IRowStrategy strategy)
      => new RowOffsetSizeStrategy(strategy).ToOffsetStrategy();

    private static IOffsetStrategy Seek(IColumnStrategy strategy)
      => new ColumnOffsetSizeStrategy(strategy).ToOffsetStrategy();

    private static Func<ISpace, int, bool> AnyCellInRow(Func<CellValue, bool> cell)
      => (space, row) =>
      {
        for (var column = 0; column < space.Area.Size.Width; column++)
          if (cell(space[column, row]))
            return true;

        return false;
      };

    private static Func<ISpace, int, bool> AnyCellInColumn(Func<CellValue, bool> cell)
      => (space, column) =>
      {
        for (var row = 0; row < space.Area.Size.Height; row++)
          if (cell(space[column, row]))
            return true;

        return false;
      };

    private static Func<CellValue, bool> TextEquals(string text)
    {
      var needle = text.Trim();

      return cell => cell.TryGetString() is string value
        && value.Trim().Equals(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int extent, string parameter)
      => extent >= 0 ? extent : throw new ArgumentOutOfRangeException(parameter, extent, "An extent cannot be negative.");
  }
}
