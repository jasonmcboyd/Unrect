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

    public Size GetSize(ISpace availableSpace)
    {
      var total = new Size(0, 0);

      foreach (var strategy in Strategies)
      {
        var offset = strategy.GetOffset(availableSpace);

        if (offset.Width > availableSpace.Area.Width || offset.Height > availableSpace.Area.Height)
          throw new OutOfBoundsException();

        total += offset.Size;
        availableSpace = availableSpace.GetSubspace(offset);
      }

      return total;
    }
  }
}
