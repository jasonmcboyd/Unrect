using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The first cell with a value, reading spans in order and each span across: the region starts there. No content anywhere skips past every span.</summary>
  internal sealed class SkipToFirstNonBlankCellStrategy : IOffsetStrategy
  {
    public IOffsetScan Begin(Orientation along) => new Scan(along);

    private sealed class Scan : IOffsetScan
    {
      private readonly Orientation _along;

      internal Scan(Orientation along) => _along = along;

      public bool Incremental => true;

      public OffsetStep Next(Plane<ISpace> region, int index, out int across)
      {
        var reach = Scanning.Across(region, _along);

        for (across = 0; across < reach; across++)
          if (Scanning.Cell(region, index, across, _along).HasValue)
            return OffsetStep.StartHere;

        across = 0;
        return OffsetStep.Skip;
      }

      public Offset Settle(Plane<ISpace> region)
        => _along == Orientation.Vertical ? new Offset(0, region.Area.Height) : new Offset(region.Width, 0);
    }
  }
}
