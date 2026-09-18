using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitSizeStrategy : ISizeStrategy
  {
    public ExplicitSizeStrategy(int width, int height)
    {
      if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
      if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

      Width = width;
      Height = height;
    }

    internal int Width { get; }

    internal int Height { get; }

    public ISizeScan Begin(Orientation along) => new Scan(new Size(Width, Height), along);

    /// <summary>Takes exactly the spans declared and is as wide as declared, whatever the region holds: what does not fit is the caller's to report.</summary>
    private sealed class Scan : ISizeScan
    {
      private readonly Size _declared;
      private readonly Orientation _along;

      internal Scan(Size declared, Orientation along)
      {
        _declared = declared;
        _along = along;
      }

      public bool Incremental => true;

      public bool Take(Plane<ISpace> region, int taken) => taken < Scanning.Along(_declared, _along);

      public int? Across(Plane<ISpace> region, int taken, bool final) => Scanning.Across(_declared, _along);

      public int Along(Plane<ISpace> region, int taken) => Scanning.Along(_declared, _along);

      public bool Complete(int taken) => taken == Scanning.Along(_declared, _along);

      public Size Declared => _declared;
    }
  }
}
