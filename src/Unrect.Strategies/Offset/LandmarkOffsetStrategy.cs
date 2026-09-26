using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A landmark lifted into an offset: onto the line it matches, or past it. Along the landmark's
  /// own axis the lift streams, answering as each line arrives; across it, it answers over the
  /// whole region. A landmark with no match is <see cref="AnchorNotFoundException"/>, worded by the
  /// landmark.
  /// </summary>
  internal sealed class LandmarkOffsetStrategy : IOffsetStrategy
  {
    public LandmarkOffsetStrategy(ILineLandmark landmark, bool past)
    {
      Landmark = landmark;
      Past = past;
    }

    internal ILineLandmark Landmark { get; }

    internal bool Past { get; }

    public IOffsetScan Begin(Orientation along)
      => along == Landmark.Along
        ? new Scan(this)
        : new Scanning.WholeOffset(region => Spans.Step(Find(region), Landmark.Along));

    private int Find(Plane<ISpace> region)
      => Landmark.Find(region) is int line
        ? line + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);

    private sealed class Scan : IOffsetScan
    {
      private readonly LandmarkOffsetStrategy _strategy;

      internal Scan(LandmarkOffsetStrategy strategy) => _strategy = strategy;

      public bool Incremental => true;

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        across = 0;

        if (_strategy.Landmark.Find(region) is null)
          return OffsetStep.Skip;

        return _strategy.Past ? OffsetStep.StartNext : OffsetStep.StartHere;
      }

      public Offset Settle(Plane<ISpace> region) => Spans.Step(_strategy.Find(region), _strategy.Landmark.Along);
    }
  }
}
