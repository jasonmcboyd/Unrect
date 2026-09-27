using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A landmark lifted into an offset: onto the line it matches, or past it. Along the landmark's
  /// own axis the lift streams, answering as each line arrives; across it, it answers over the
  /// whole region. A landmark with no match is <see cref="AnchorNotFoundException"/>, worded by the
  /// landmark.
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

    public IOffsetScan<TSpace> Begin(Orientation along)
      => along == Landmark.Along
        ? new Scan(this)
        : new Scanning.WholeOffset<TSpace>(region => Spans.Step(Find(region), Landmark.Along));

    private int Find(Plane<TSpace> region)
      => Landmark.Find(region) is int line
        ? line + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);

    private sealed class Scan : IOffsetScan<TSpace>
    {
      private readonly LandmarkOffsetStrategy<TSpace> _strategy;

      internal Scan(LandmarkOffsetStrategy<TSpace> strategy) => _strategy = strategy;

      public bool Incremental => true;

      public OffsetStep Next(Plane<TSpace> region, int index, out int across)
      {
        across = 0;

        if (_strategy.Landmark.Find(region) is null)
          return OffsetStep.Skip;

        return _strategy.Past ? OffsetStep.StartNext : OffsetStep.StartHere;
      }

      public Offset Settle(Plane<TSpace> region) => Spans.Step(_strategy.Find(region), _strategy.Landmark.Along);
    }
  }
}
