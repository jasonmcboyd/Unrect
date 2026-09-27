using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A landmark lifted into an offset: onto the line it matches, or past it. Along the landmark's
  /// own axis the lift streams, answering as each line arrives — the gap ends on the matched line,
  /// or takes it in and ends on the next; across it, it answers over the whole region. A landmark
  /// with no match is <see cref="AnchorNotFoundException"/>, worded by the landmark.
  /// </summary>
  internal sealed class LandmarkOffsetStrategy<TSpace> : IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public LandmarkOffsetStrategy(ILineLandmark<TSpace> landmark, bool past)
    {
      Landmark = landmark;
      Past = past;
    }

    internal ILineLandmark<TSpace> Landmark { get; }

    internal bool Past { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
      => along == Landmark.Along
        ? new Scan(this)
        : new Scanning.WholeSize<TSpace>(region => Spans.ToSize(Find(region), 0, Landmark.Along), along);

    private int Find(Plane<TSpace> region)
      => Landmark.Find(region) is int line
        ? line + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);

    private sealed class Scan : ISizeScan<TSpace>
    {
      private readonly LandmarkOffsetStrategy<TSpace> _strategy;
      private bool _matched;

      internal Scan(LandmarkOffsetStrategy<TSpace> strategy) => _strategy = strategy;

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int taken)
      {
        if (_matched)
          return false;

        if (_strategy.Landmark.Find(region) is null)
          return true;

        _matched = true;
        return _strategy.Past;
      }

      public int? Across(Plane<TSpace> region, int taken, bool final) => 0;

      public int Along(Plane<TSpace> region, int taken)
        => _matched ? taken : throw new AnchorNotFoundException(_strategy.Landmark.Description);

      public Size? Required => null;
    }
  }
}
