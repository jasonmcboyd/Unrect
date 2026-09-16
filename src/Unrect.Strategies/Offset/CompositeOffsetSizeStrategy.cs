using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Applies each offset to the space left by the one before it and sums the results, so
  /// <c>Then(SkipBlankRows(), ExplicitOffset(0, 1))</c> reads as "past the blank band, then one
  /// more row".
  /// </summary>
  internal sealed class CompositeOffsetSizeStrategy : ISizeStrategy
  {
    public CompositeOffsetSizeStrategy(IOffsetStrategy[] strategies)
    {
      if (strategies is null) throw new ArgumentNullException(nameof(strategies));

      Strategies = (IOffsetStrategy[])strategies.Clone();

      foreach (var strategy in Strategies)
        if (strategy is null)
          throw new ArgumentException("An offset strategy is null.", nameof(strategies));
    }

    private IOffsetStrategy[] Strategies { get; }

    public Size GetSize(Plane<ISpace> availableSpace)
    {
      var total = new Size(0, 0);

      foreach (var strategy in Strategies)
      {
        var offset = strategy.GetOffset(availableSpace);

        total += offset.Size;

        // Arithmetic, not a new subspace object — a canonical region cannot cut one. The READS
        // that follow are identical: the same cells of the same space at translated coordinates.
        // What differs is the band a windowed space announces to its store, which is now the
        // parent subspace object's rather than this step's. That is observable only where the
        // first strategy of a composition reads nothing — RowsThenColumns(TakeRows(2), …) — and
        // it is exactly what the engine will announce once it announces once per placement.
        // Slice also makes the bounds check this used to make for itself, without settling a bound.
        availableSpace = availableSpace.Slice(offset);
      }

      return total;
    }
  }
}
