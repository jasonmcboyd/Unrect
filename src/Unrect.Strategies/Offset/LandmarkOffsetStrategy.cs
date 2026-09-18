using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The row a landmark matches, or the one after it: the region starts there, and a landmark
  /// that matches nothing is a missing anchor rather than a place. Streams down the rows —
  /// the landmark is handed the rows seen so far and can only match on the newest — and answers
  /// over the whole region when asked along the columns.
  /// </summary>
  internal sealed class RowLandmarkOffsetStrategy : IOffsetStrategy
  {
    public RowLandmarkOffsetStrategy(IRowLandmark landmark, bool past)
    {
      Landmark = landmark;
      Past = past;
    }

    internal IRowLandmark Landmark { get; }

    internal bool Past { get; }

    public IOffsetScan Begin(Orientation along)
      => along == Orientation.Vertical
        ? new Scan(this)
        : new Scanning.WholeOffset(region => new Offset(0, Find(region)));

    private int Find(Plane<ISpace> region)
      => Landmark.FindRow(region) is int row
        ? row + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);

    private sealed class Scan : IOffsetScan
    {
      private readonly RowLandmarkOffsetStrategy _strategy;

      internal Scan(RowLandmarkOffsetStrategy strategy) => _strategy = strategy;

      public bool Incremental => true;

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        across = 0;

        if (_strategy.Landmark.FindRow(region) is null)
          return OffsetStep.Skip;

        return _strategy.Past ? OffsetStep.StartNext : OffsetStep.StartHere;
      }

      public Offset Settle(Plane<ISpace> region) => new Offset(0, _strategy.Find(region));
    }
  }

  /// <summary>The column twin: the column a landmark matches, streaming across the columns.</summary>
  internal sealed class ColumnLandmarkOffsetStrategy : IOffsetStrategy
  {
    public ColumnLandmarkOffsetStrategy(IColumnLandmark landmark, bool past)
    {
      Landmark = landmark;
      Past = past;
    }

    internal IColumnLandmark Landmark { get; }

    internal bool Past { get; }

    public IOffsetScan Begin(Orientation along)
      => along == Orientation.Horizontal
        ? new Scan(this)
        : new Scanning.WholeOffset(region => new Offset(Find(region), 0));

    private int Find(Plane<ISpace> region)
      => Landmark.FindColumn(region) is int column
        ? column + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);

    private sealed class Scan : IOffsetScan
    {
      private readonly ColumnLandmarkOffsetStrategy _strategy;

      internal Scan(ColumnLandmarkOffsetStrategy strategy) => _strategy = strategy;

      public bool Incremental => true;

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        across = 0;

        if (_strategy.Landmark.FindColumn(region) is null)
          return OffsetStep.Skip;

        return _strategy.Past ? OffsetStep.StartNext : OffsetStep.StartHere;
      }

      public Offset Settle(Plane<ISpace> region) => new Offset(_strategy.Find(region), 0);
    }
  }
}
