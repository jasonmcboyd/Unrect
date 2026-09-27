using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The first cell with a value, reading spans in order and each span across: the region starts there. No content anywhere skips past every span.</summary>
  internal sealed class SkipToFirstNonBlankCellStrategy<TSpace> : IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public ISizeScan<TSpace> Begin(Orientation along) => new Scan(along);

    private sealed class Scan : ISizeScan<TSpace>
    {
      private readonly Orientation _along;
      private int _across;

      internal Scan(Orientation along) => _along = along;

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int taken)
      {
        var reach = Spans.Across(region, _along);

        for (var across = 0; across < reach; across++)
        {
          if (!Spans.Cell(region, taken, across, _along).IsBlank())
          {
            _across = across;
            return false;
          }
        }

        return true;
      }

      public int? Across(Plane<TSpace> region, int taken, bool final) => _across;

      public int Along(Plane<TSpace> region, int taken) => taken;

      public Size? Required => null;
    }
  }
}
