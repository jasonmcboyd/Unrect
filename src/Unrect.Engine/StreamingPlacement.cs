using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a driven child's placement decides, span by span: where it starts, whether it takes the
  /// next span, and how wide it is once that can be known. Owns the placement's two rules and the
  /// wording of every failure they can raise, so the handle around it is left with the protocol —
  /// which spans were offered, which were kept, what the inner machine was fed.
  /// </summary>
  internal sealed class StreamingPlacement<TSpace>
    where TSpace : class, ISpace
  {
    private readonly OffsetRule? _offsetRule;
    private readonly SizeRule? _sizeRule;
    private readonly IProjectionDefinition _definition;
    private readonly Orientation _driver;
    private readonly bool _strict;
    private int _skipped;

    internal StreamingPlacement(OffsetRule? offsetRule, SizeRule? sizeRule, bool derived, IProjectionDefinition definition, Orientation driver, bool strict)
    {
      _offsetRule = offsetRule;
      _sizeRule = sizeRule;
      Derived = derived;
      _definition = definition;
      _driver = driver;
      _strict = strict;
    }

    /// <summary>Whether the child's own machine decides its extent — no size rule, every span it accepts is its.</summary>
    internal bool Derived { get; }

    /// <summary>The offset the child starts at, once <see cref="Advance"/> has said where.</summary>
    internal Offset Offset { get; private set; }

    /// <summary>The width across the driver's axis, once the size rule has settled it.</summary>
    internal int? Width { get; private set; }

    /// <summary>What the size rule declared, for a message about an extent that does not fit.</summary>
    internal Size Declared => _sizeRule!.Declared;

    /// <summary>
    /// Set when a placement failed and the child was started non-strictly: the parent reads the
    /// refusal off the handle rather than catching a failure. Strict placements throw instead.
    /// </summary>
    internal bool Failed { get; private set; }

    /// <summary>
    /// The offset rule's answer for the newest span: skip it, start on it, start on the one after,
    /// or refuse — the last with <see cref="Failed"/> set. <paramref name="region"/> is every span
    /// offered so far; <paramref name="across"/> is how far the newest one reaches across the
    /// driver's axis, which a column offset must fit inside.
    /// </summary>
    internal OffsetStep Advance(Plane<ISpace> region, int index, int across, ProjectorScope<TSpace> parent, out bool refused)
    {
      refused = false;
      OffsetStep step;
      int column;

      try
      {
        step = _offsetRule!.Next(region, index, out column);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (_strict)
          throw parent.Failure(_definition, EngineRules.Missing(exception), region, null, exception);

        Failed = refused = true;
        return OffsetStep.Skip;
      }
      catch (Exception exception)
      {
        throw parent.Failure(_definition, EngineRules.Threw("offset", exception), region, null, exception, EngineRules.IsFault(exception));
      }

      if (step == OffsetStep.Skip)
      {
        _skipped++;
        return step;
      }

      if (step == OffsetStep.StartNext)
        _skipped++;

      Offset = _driver == Orientation.Vertical ? new Offset(column, _skipped) : new Offset(_skipped, column);

      if (column > across)
      {
        if (_strict)
          throw parent.Failure(_definition, $"an offset of {EngineRules.Describe(Offset.Size)} does not fit the available space", region, Offset.Size, null);

        Failed = refused = true;
      }

      return step;
    }

    /// <summary>Whether the size rule takes the newest span of <paramref name="region"/>, the <paramref name="taken"/>th; false with <see cref="Failed"/> set is a refusal.</summary>
    internal bool Take(Plane<ISpace> region, int taken, ProjectorScope<TSpace> child)
    {
      try
      {
        return _sizeRule!.Take(region, taken);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (_strict)
          throw EngineRules.AreaFailure(child, _definition, region, exception);

        Failed = true;
        return false;
      }
      catch (Exception exception)
      {
        throw EngineRules.AreaFailure(child, _definition, region, exception);
      }
    }

    /// <summary>
    /// Asks the size rule for the width over <paramref name="region"/>, the rows taken so far, and
    /// says whether it settled it just now. Too wide is reported only once the rows have settled,
    /// so a declared 3x3 on a 2x2 space says "2x2 available" rather than the one row it had seen
    /// when the width came in.
    /// </summary>
    internal bool TrySettleWidth(Plane<ISpace> region, int taken, bool rowsSettled, ProjectorScope<TSpace> child)
    {
      int? width;

      try
      {
        width = _sizeRule!.Width(region, taken, rowsSettled);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (_strict)
          throw EngineRules.AreaFailure(child, _definition, region, exception);

        Failed = true;
        return false;
      }
      catch (Exception exception)
      {
        throw EngineRules.AreaFailure(child, _definition, region, exception);
      }

      if (width is not int settled)
        return false;

      if (settled > Spans.Across(region.Area.Size, _driver))
      {
        if (!rowsSettled)
          return false;

        var size = _sizeRule.Declared.Height > 0 ? _sizeRule.Declared : new Size(settled, taken);

        if (_strict)
          throw child.Failure(_definition, $"an extent of {EngineRules.Describe(size)} does not fit here", region, size, null);

        Failed = true;
        return false;
      }

      Width = settled;
      return true;
    }

    /// <summary>Whether <paramref name="taken"/> spans satisfy the size rule — an explicit height wants all of them.</summary>
    internal bool Complete(int taken) => _sizeRule!.Complete(taken);
  }
}
