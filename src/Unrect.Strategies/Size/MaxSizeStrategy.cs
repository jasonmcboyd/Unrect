using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class MaxSizeStrategy : ISizeStrategy
  {
    public ISizeScan Begin(Orientation along) => new Scan(along);

    /// <summary>Everything: every span, as far across as the region reaches.</summary>
    private sealed class Scan : ISizeScan
    {
      private readonly Orientation _along;

      internal Scan(Orientation along) => _along = along;

      public bool Incremental => true;

      public bool Take(Plane<ISpace> region, int taken) => true;

      public int? Across(Plane<ISpace> region, int taken, bool final) => Spans.Across(region, _along);

      public int Along(Plane<ISpace> region, int taken) => taken;

      public bool Complete(int taken) => true;

      public Size Declared => default;
    }
  }
}
