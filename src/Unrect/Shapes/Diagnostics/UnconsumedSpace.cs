using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The one diagnostic a successful parse can still raise: the cells nobody described. It is how a
  /// declaration that has drifted from its file says so without failing.
  /// </summary>
  public static partial class ShapeExtensions
  {
    /// <summary>
    /// Space left over is the shape drifting from the file — the cells nobody described are exactly
    /// where the next surprise lives. A leading offset is a gap as much as a trailing remainder is:
    /// a shape that starts two rows down described neither those two rows nor whatever follows it.
    /// </summary>
    private static void ReportUnconsumed(IShape shape, ISpace space, Size gap, Size described, ShapeContext context)
    {
      var size = space.Area.Size;

      if (described.Width >= size.Width && described.Height >= size.Height)
        return;

      var counts = new List<string>(2);
      var undescribed = new List<string>(2);

      Describe(gap.Height, described.Height, size.Height, "row", counts, undescribed);
      Describe(gap.Width, described.Width, size.Width, "column", counts, undescribed);


      // The earliest cell nothing described, in reading order: a leading gap on either axis starts
      // at the very first cell, otherwise it is wherever the described region stops.
      var first =
        gap.Width > 0 || gap.Height > 0 ? default
        : described.Width < size.Width ? new Offset(described.Width, 0)
        : new Offset(0, described.Height);

      context.Advance(first).Report(
        DiagnosticSeverity.Info,
        shape,
        $"the shape consumed {string.Join(" and ", counts)}; {string.Join(" and ", undescribed)} were not described",
        space);
    }

    /// <summary>
    /// Adds one axis's worth of what was read and what was skipped, before it and after it — in
    /// 1-based terms, because the reader is looking at a spreadsheet.
    /// </summary>
    private static void Describe(int gap, int described, int total, string axis, List<string> counts, List<string> undescribed)
    {
      if (described >= total)
        return;

      counts.Add($"{described} of {total} {axis}s");

      var ranges = new List<string>(2);

      if (gap > 0)
        ranges.Add(gap == 1 ? "1" : $"1-{gap}");

      var after = gap + described;

      if (after < total)
        ranges.Add($"{after + 1}+");

      undescribed.Add($"{axis}s {string.Join(" and ", ranges)}");
    }
  }
}
