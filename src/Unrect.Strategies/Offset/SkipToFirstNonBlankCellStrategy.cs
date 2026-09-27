using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The first cell with a value, reading spans in order and each span across: the region starts there. No content anywhere skips past every span.</summary>
  internal sealed class SkipToFirstNonBlankCellStrategy<TSpace> : IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public IOffsetScan<TSpace> Begin(Orientation along) => new Scan(along);

    private sealed class Scan : IOffsetScan<TSpace>
    {
      private readonly Orientation _along;

      internal Scan(Orientation along) => _along = along;

      public bool Incremental => true;

      public OffsetStep Next(Plane<TSpace> region, int index, out int across)
      {
        var reach = Spans.Across(region, _along);

        for (across = 0; across < reach; across++)
          if (!Spans.Cell(region, index, across, _along).IsBlank())
            return OffsetStep.StartHere;

        across = 0;
        return OffsetStep.Skip;
      }

      public Offset Settle(Plane<TSpace> region)
        => _along == Orientation.Vertical ? new Offset(0, region.Height) : new Offset(region.Width, 0);
    }
  }
}
