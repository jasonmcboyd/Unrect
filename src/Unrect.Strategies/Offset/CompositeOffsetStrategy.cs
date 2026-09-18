using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Offsets applied one after another, each from where the last one left off: a chain of scans
  /// along the axis, each stage starting on the span its predecessor started on (or the one
  /// after), and reading its spans from that stage's own corner.
  /// </summary>
  internal sealed class CompositeOffsetStrategy : IOffsetStrategy
  {
    public CompositeOffsetStrategy(IOffsetStrategy[] strategies)
    {
      if (strategies is null) throw new ArgumentNullException(nameof(strategies));

      Strategies = (IOffsetStrategy[])strategies.Clone();

      foreach (var strategy in Strategies)
        if (strategy is null)
          throw new ArgumentException("An offset strategy is null.", nameof(strategies));
    }

    internal IOffsetStrategy[] Strategies { get; }

    public IOffsetScan Begin(Orientation along)
    {
      var stages = new IOffsetScan[Strategies.Length];

      for (var index = 0; index < stages.Length; index++)
        stages[index] = Strategies[index].Begin(along);

      return new Chain(stages, along);
    }

    private sealed class Chain : IOffsetScan
    {
      private readonly IOffsetScan[] _stages;
      private readonly Orientation _along;
      private int _stage;
      private int _stageStart;
      private int _across;

      internal Chain(IOffsetScan[] stages, Orientation along)
      {
        _stages = stages;
        _along = along;
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

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        if (_stages.Length == 0)
        {
          across = 0;
          return OffsetStep.StartHere;
        }

        while (true)
        {
          var stageRegion = region.Slice(_along == Orientation.Vertical ? new Offset(_across, _stageStart) : new Offset(_stageStart, _across));
          var step = _stages[_stage].Next(stageRegion, index - _stageStart, out var stageAcross);

          if (step == OffsetStep.Skip)
          {
            across = _across;
            return OffsetStep.Skip;
          }

          _across += stageAcross;
          across = _across;

          if (_stage == _stages.Length - 1)
            return step;

          _stage++;

          if (step == OffsetStep.StartHere)
          {
            _stageStart = index;
            continue;
          }

          _stageStart = index + 1;
          return OffsetStep.Skip;
        }
      }

      public Offset Settle(Plane<ISpace> region)
      {
        // Each stage over what the last one left: the same arithmetic the streaming form does,
        // stated over the whole region for a chain whose stages need it that way.
        var total = new Size(0, 0);

        foreach (var stage in _stages)
        {
          var offset = Scans.FoldOffset(stage, region, _along);
          total += offset.Size;
          region = region.Slice(offset);
        }

        return new Offset(total);
      }
    }
  }
}
