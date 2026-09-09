using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One declared item applied as many times as the space supports. The separator sits between
  /// items and never before the first; a leading gap is the repeat's own offset.
  /// </summary>
  internal sealed class RepeatProjection<T> : ProjectionBase<IReadOnlyList<T>>
  {
    public RepeatProjection(
      IProjection<T> item,
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
      Children = new IProjection[] { item };
    }

    private IProjection<T> Item { get; }

    /// <summary>What the declaration called the item, for every occurrence of it to be labelled by.</summary>
    private UseSite ItemSite { get; }
    private IOffsetStrategy? Separator { get; }
    private Orientation Orientation { get; }
    private int AtLeast { get; }

    public override string Description => Orientation == Orientation.Vertical ? "VerticalRepeat" : "HorizontalRepeat";

    public override IReadOnlyList<IProjection> Children { get; }

    public override ProjectionResult<IReadOnlyList<T>> Project(ISpace extent, ProjectionContext context)
    {
      var values = new List<T>();
      var along = 0;
      var across = 0;
      var absorbed = false;

      while (true)
      {
        var mark = context.Diagnostics.Mark();

        if (!TryCollect(extent, context, values, ref along, ref across, ref absorbed))
        {
          // An attempt that is not collected leaves nothing behind — not even what it tolerated on
          // the way to being discarded.
          context.Diagnostics.Rollback(mark);
          break;
        }
      }

      // Reported after that rollback, and it is the only thing left saying so: the tolerated
      // failure's own warning went with the attempt that produced it.
      if (absorbed)
        context.Report(DiagnosticSeverity.Info, this, EndedByTolerance(values.Count), extent);

      if (values.Count < AtLeast)
        throw context.Failure($"expected at least {AtLeast} occurrences but found {values.Count}", extent);

      // Zero occurrences is a statement about the data — the repetition looked and the data held
      // none of these — which an enclosing shape reads as Empty rather than inferring from a zero.
      return new ProjectionResult<IReadOnlyList<T>>(
        values,
        Extent(along, across),
        values.Count == 0 ? Presence.Empty : Presence.Read);
    }

    /// <summary>
    /// The guided form of a documented trap: a repetition that ran out of productivity on an item
    /// whose failure had just been absorbed. It cannot tell "no more of these" from "one of these
    /// was broken", so the declaration almost certainly means something else.
    /// <para>
    /// It says why the run ended; it does not end it. Only the productivity guard does that, so an
    /// absorbed item that still consumed a declared extent goes on repeating and says nothing here
    /// — the trap needs both halves, the tolerance and the standstill.
    /// </para>
    /// <para>
    /// The occurrence is bracketed as the paths render it, so it lines up with the
    /// <c>VerticalRepeat[n]</c> segments a reader is already looking at.
    /// </para>
    /// </summary>
    private static string EndedByTolerance(int occurrence)
      => $"the repetition ended at occurrence [{occurrence}]: the item's failure was absorbed by a tolerance "
       + "boundary, and a tolerated item cannot drive a repetition — drop the boundary, or declare "
       + "atLeast: 0 if an empty run is the concern";

    /// <summary>
    /// One attempt: separate, place, project, and collect. False means the repetition is over, and
    /// whatever the attempt did is discarded by the caller — which is why <paramref name="absorbed"/>
    /// travels back out here rather than being reported in place.
    /// </summary>
    private bool TryCollect(ISpace extent, ProjectionContext context, List<T> values, ref int along, ref int across, ref bool absorbed)
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
      if (!ProjectionEngine.TryApply(Item, remaining, scope, out var applied))
        return false;

      // An item that occupies nothing, or advances nowhere, would repeat forever. This number is
      // the sole decider, and deliberately: presence explains a stop, it never causes one.
      // A boundary under a declared area absorbs and still consumes the extent the placement
      // claimed, so a repeat of item.Optional().Sized(…) goes on collecting tolerated defaults —
      // which is what it did before presence existed, and changing that would be a semantic change
      // wearing a diagnostic's clothes.
      if (applied.Consumed.Width == 0 || applied.Consumed.Height == 0 || Along(applied.Advance) == 0)
      {
        // Read on the way out: why the item that ended the run had nothing to give.
        absorbed = applied.Presence == Presence.Absorbed;
        return false;
      }

      values.Add(applied.Value);
      along = cursor + Along(applied.Advance);
      across = Math.Max(reach, Across(applied.Advance));
      return true;
    }

    private bool TrySeparate(ISpace remaining, ProjectionContext context, ref int cursor, ref int reach)
    {
      if (Separator is null)
        return true;

      Offset offset;
      try
      {
        offset = Separator.GetOffset(remaining);
      }
      catch (ProjectionException)
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
        throw context.Failure(ProjectionEngine.Threw("separator", exception), remaining, exception, ProjectionEngine.IsFault(exception));
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
