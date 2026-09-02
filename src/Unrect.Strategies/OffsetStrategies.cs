using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  public static class OffsetStrategies
  {

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

    // --- The two lifts: where a matcher puts a shape ---------------------------------------------
    //
    // A skip-while stops at the first row that fails its predicate, so anything inserted above the
    // thing you are looking for moves it. A matcher scans to the first row that matches instead,
    // which is what survives that. It locates content and reports absence without deciding what
    // absence means; these two lifts decide it for a placement — the anchor was required. That
    // answer arrives as an OutOfBoundsException from an offset strategy, which is how a strict
    // shape reports a missing anchor and how a repeat learns there are no more sections.

    /// <summary>
    /// Onto the row <paramref name="landmark"/> matches. The region starts AT that row, so the
    /// shape owns it — a caption its section should describe, or a label row it reads.
    /// </summary>
    public static IOffsetStrategy To(IRowLandmark landmark)
      => Lift(new LandmarkRowStrategy(NotNull(landmark, nameof(landmark)), past: false));

    /// <summary>Onto the column <paramref name="landmark"/> matches; the column twin of <see cref="To(IRowLandmark)"/>.</summary>
    public static IOffsetStrategy To(IColumnLandmark landmark)
      => Lift(new LandmarkColumnStrategy(NotNull(landmark, nameof(landmark)), past: false));

    /// <summary>
    /// Onto the row after the one <paramref name="landmark"/> matches, for a shape that starts
    /// below a row it does not want to own. This is the whole of the old anchor-then-skip idiom,
    /// without the hard-coded 1 that stood in for the matched row's own height.
    /// </summary>
    public static IOffsetStrategy Past(IRowLandmark landmark)
      => Lift(new LandmarkRowStrategy(NotNull(landmark, nameof(landmark)), past: true));

    /// <summary>Onto the column after the match; the column twin of <see cref="Past(IRowLandmark)"/>.</summary>
    public static IOffsetStrategy Past(IColumnLandmark landmark)
      => Lift(new LandmarkColumnStrategy(NotNull(landmark, nameof(landmark)), past: true));

    // --- Anchoring to the far edge --------------------------------------------------------------
    //
    // Both measure back from the end of the available space, so they are normally spelled with
    // .After(...), which replaces: composing a movement before a from-end anchor rarely means
    // anything, since the anchor discards where the movement left off.

    /// <summary>The rightmost <paramref name="width"/> columns of the available space.</summary>
    public static IOffsetStrategy FromRight(int width)
    {
      NotNegative(width, nameof(width));

      return SelectOffset(space => new Size(Reserve(space.Area.Width, width), 0));
    }

    /// <summary>The bottom <paramref name="height"/> rows of the available space.</summary>
    public static IOffsetStrategy FromBottom(int height)
    {
      NotNegative(height, nameof(height));

      return SelectOffset(space => new Size(0, Reserve(space.Area.Height, height)));
    }

    /// <summary>How far in to start so that <paramref name="extent"/> reaches the far edge.</summary>
    private static int Reserve(int available, int extent)
      => extent <= available ? available - extent : throw new OutOfBoundsException();

    private static IOffsetStrategy Lift(IRowStrategy strategy)
      => new RowOffsetSizeStrategy(strategy).ToOffsetStrategy();

    private static IOffsetStrategy Lift(IColumnStrategy strategy)
      => new ColumnOffsetSizeStrategy(strategy).ToOffsetStrategy();

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int extent, string parameter)
      => extent >= 0 ? extent : throw new ArgumentOutOfRangeException(parameter, extent, "An extent cannot be negative.");
  }
}
