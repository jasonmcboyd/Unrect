using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class MaxSizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public ISizeScan<TSpace> Begin(Orientation along) => new Scan(along);

    /// <summary>Everything: every span, as far across as the region reaches.</summary>
    private sealed class Scan : ISizeScan<TSpace>
    {
      private readonly Orientation _along;

      internal Scan(Orientation along) => _along = along;

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int taken) => true;

      public int? Across(Plane<TSpace> region, int taken, bool final) => Spans.Across(region, _along);

      public int Along(Plane<TSpace> region, int taken) => taken;

      public Size? Required => null;
    }
  }
}
