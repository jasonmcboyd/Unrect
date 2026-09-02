using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// One declared item applied as many times as the space supports. The separator sits between
  /// items and never before the first; a leading gap is the repeat's own offset.
  /// </summary>
  internal sealed class RepeatShape<T> : ShapeBase<IReadOnlyList<T>>
  {
    public RepeatShape(
      IShape<T> item,
      IOffsetStrategy? separator,
      Orientation orientation,
      int atLeast,
      UseSite itemSite,
      Placement placement)
      : base(placement)
    {
      Item = item ?? throw new ArgumentNullException(nameof(item));
      Separator = separator;
      Orientation = orientation;
      AtLeast = atLeast;
      ItemSite = itemSite;
      Children = new IShape[] { item };
    }

    private IShape<T> Item { get; }

    /// <summary>What the declaration called the item, for every occurrence of it to be labelled by.</summary>
    private UseSite ItemSite { get; }
    private IOffsetStrategy? Separator { get; }
    private Orientation Orientation { get; }
    private int AtLeast { get; }

    public override string Description => Orientation == Orientation.Vertical ? "Repeat" : "RepeatHorizontal";

    public override IReadOnlyList<IShape> Children { get; }

    public override ShapeResult<IReadOnlyList<T>> Project(ISpace extent, ShapeContext context)
    {
      var values = new List<T>();
      var along = 0;
      var across = 0;

      while (true)
      {
        var mark = context.Diagnostics.Mark();

        if (!TryCollect(extent, context, values, ref along, ref across))
        {
          // An attempt that is not collected leaves nothing behind — not even what it tolerated on
          // the way to being discarded.
          context.Diagnostics.Rollback(mark);
          break;
        }
      }

      if (values.Count < AtLeast)
        throw context.Failure($"expected at least {AtLeast} occurrences but found {values.Count}", extent);

      return new ShapeResult<IReadOnlyList<T>>(values, Extent(along, across));
    }

    /// <summary>
    /// One attempt: separate, place, project, and collect. False means the repetition is over, and
    /// whatever the attempt did is discarded by the caller.
    /// </summary>
    private bool TryCollect(ISpace extent, ShapeContext context, List<T> values, ref int along, ref int across)
    {
      // The cursor is tentative until an item is collected, so a separator followed by nothing
      // (a trailing blank band) is not counted as consumed.
      var cursor = along;
      var reach = across;

      if (values.Count > 0 && !TrySeparate(extent.GetSubspace(Step(cursor)), context, ref cursor, ref reach))
        return false;

      var remaining = extent.GetSubspace(Step(cursor));

      if (IsEmpty(remaining))
        return false;

      // The index belongs to the repeat's own segment; the label belongs to the item, which claims
      // it on the way in. Descend clears it afterwards, so the item's own children are unaffected.
      var scope = context.Advance(Step(cursor)).WithIndex(values.Count).WithUseSite(ItemSite);

      // Only the item's own placement stops the repetition; a failure deeper inside it is an
      // error, so intra-block format drift is loud rather than silently truncating.
      if (!ShapeEngine.TryApply(Item, remaining, scope, out var applied))
        return false;

      // An item that occupies nothing, or advances nowhere, would repeat forever.
      if (applied.Consumed.Width == 0 || applied.Consumed.Height == 0 || Along(applied.Advance) == 0)
        return false;

      values.Add(applied.Value);
      along = cursor + Along(applied.Advance);
      across = Math.Max(reach, Across(applied.Advance));
      return true;
    }

    private bool TrySeparate(ISpace remaining, ShapeContext context, ref int cursor, ref int reach)
    {
      if (Separator is null)
        return true;

      Offset offset;
      try
      {
        offset = Separator.GetOffset(remaining);
      }
      catch (ShapeException)
      {
        throw;
      }
      catch (OutOfBoundsException)
      {
        // No room for another separator, so there is no room for another item.
        return false;
      }
      catch (Exception exception)
      {
        throw context.Failure(ShapeEngine.Threw("separator", exception), remaining, exception);
      }

      if (offset.Width > remaining.Area.Width || offset.Height > remaining.Area.Height)
        return false;

      cursor += Along(offset.Size);
      reach = Math.Max(reach, Across(offset.Size));
      return true;
    }

    private static bool IsEmpty(ISpace space) => space.Area.Width == 0 || space.Area.Height == 0;

    private Offset Step(int along) => Orientation == Orientation.Vertical ? new Offset(0, along) : new Offset(along, 0);

    private int Along(Size size) => Orientation == Orientation.Vertical ? size.Height : size.Width;

    private int Across(Size size) => Orientation == Orientation.Vertical ? size.Width : size.Height;

    private Size Extent(int along, int across)
      => Orientation == Orientation.Vertical ? new Size(across, along) : new Size(along, across);
  }
}
