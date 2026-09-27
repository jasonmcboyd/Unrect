using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Offsets applied one after another, each from where the last one left off: a chain of scans
  /// along the axis, each stage's gap starting on the span its predecessor's ended, and reading
  /// its spans from that stage's own corner.
  /// </summary>
  internal sealed class CompositeOffsetStrategy<TSpace> : IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public CompositeOffsetStrategy(IOffsetStrategy<TSpace>[] strategies)
    {
      if (strategies is null) throw new ArgumentNullException(nameof(strategies));

      Strategies = (IOffsetStrategy<TSpace>[])strategies.Clone();

      foreach (var strategy in Strategies)
        if (strategy is null)
          throw new ArgumentException("An offset strategy is null.", nameof(strategies));
    }

    internal IOffsetStrategy<TSpace>[] Strategies { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
    {
      var stages = new ISizeScan<TSpace>[Strategies.Length];

      for (var index = 0; index < stages.Length; index++)
        stages[index] = Strategies[index].Begin(along);

      return new Chain(stages, along);
    }

    /// <summary>
    /// The stages driven in turn: a stage takes spans until its gap ends, is settled over what it
    /// took, and the next stage is shown the span it refused as its first. A stage that was driven
    /// is never folded again — a scan may carry state — so the end-of-spans settlement settles the
    /// stage in progress over what it saw and folds only the stages never shown a span.
    /// </summary>
    private sealed class Chain : ISizeScan<TSpace>
    {
      private readonly ISizeScan<TSpace>[] _stages;
      private readonly Orientation _along;
      private int _stage;
      private int _stageStart;
      private int _across;
      private bool _done;

      internal Chain(ISizeScan<TSpace>[] stages, Orientation along)
      {
        _stages = stages;
        _along = along;
        _done = stages.Length == 0;
      }

      public bool Incremental
      {
        get
        {
          foreach (var stage in _stages)
            if (!stage.Incremental)
              return false;

          return true;
        }
      }

      public bool Take(Plane<TSpace> region, int index)
      {
        while (!_done)
        {
          var stageRegion = region.Slice(Corner());
          var local = index - _stageStart;

          if (_stages[_stage].Take(stageRegion, local))
            return true;

          Advance(Scans.Settle(_stages[_stage], Spans.Prefix(stageRegion, local, _along), local, _along));
        }

        return false;
      }

      public int? Across(Plane<TSpace> region, int taken, bool final)
        => final ? Spans.Across(Settle(region, taken), _along) : (int?)null;

      public int Along(Plane<TSpace> region, int taken) => Spans.Along(Settle(region, taken), _along);

      public Size? Required => null;

      /// <summary>
      /// The stage in progress settled over what it was shown, then every stage never shown a span
      /// folded over what is left. Each stage's corner is cut from the region as the next stage's
      /// start, so a corner past the region is the chain's own bounds failure — it ran past.
      /// </summary>
      private Size Settle(Plane<TSpace> region, int taken)
      {
        if (!_done)
        {
          var stageRegion = region.Slice(Corner());

          Advance(Scans.Settle(_stages[_stage], Spans.Prefix(stageRegion, taken - _stageStart, _along), taken - _stageStart, _along));

          while (true)
          {
            stageRegion = region.Slice(Corner());

            if (_done)
              break;

            Advance(Scans.FoldSize(_stages[_stage], stageRegion, _along));
          }
        }

        return Spans.ToSize(_stageStart, _across, _along);
      }

      /// <summary>The current stage's block added to the total, and the chain moved on to the next stage or finished.</summary>
      private void Advance(Size block)
      {
        _across += Spans.Across(block, _along);
        _stageStart += Spans.Along(block, _along);

        if (_stage == _stages.Length - 1)
          _done = true;
        else
          _stage++;
      }

      private Offset Corner() => Spans.ToOffset(_stageStart, _across, _along);
    }
  }
}
