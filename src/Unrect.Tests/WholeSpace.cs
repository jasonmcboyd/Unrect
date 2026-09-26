using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// Every fixture here hands back a space, and every one of these tests means the whole of it. That
  /// is exactly <c>Plane&lt;TSpace&gt;.Of(space)</c>, so these overloads say it once rather than at six
  /// hundred call sites. They add no behaviour: a region over a whole space with origin (0, 0) is
  /// the space, and a test that wants a region of part of one still writes the slice out. A rule is
  /// applied through a plane of the space it was written over — the canonical one for a rule written
  /// at <see cref="ISpace"/>, the sheet's own for one written at <see cref="ICellSpace"/> — and a sheet
  /// is both, so either rule takes a sheet.
  /// </summary>
  internal static class WholeSpace
  {
    public static Plane<ISpace> Region(this ICellSpace space) => Plane<ISpace>.Of(space);

    public static Size GetSize<TSpace>(this ISizeStrategy<TSpace> strategy, TSpace space) where TSpace : class, ISpace => strategy.GetSize(Plane<TSpace>.Of(space));

    public static Offset GetOffset<TSpace>(this IOffsetStrategy<TSpace> strategy, TSpace space) where TSpace : class, ISpace => strategy.GetOffset(Plane<TSpace>.Of(space));

    public static int SelectLines<TSpace>(this ILineStrategy<TSpace> strategy, TSpace space) where TSpace : class, ISpace => strategy.SelectLines(Plane<TSpace>.Of(space));

    public static int? Find<TSpace>(this ILineLandmark<TSpace> landmark, TSpace space) where TSpace : class, ISpace => landmark.Find(Plane<TSpace>.Of(space));

    public static bool Includes<TSpace>(this ILineScan<TSpace> scan, TSpace space, int row) where TSpace : class, ISpace => scan.Includes(Plane<TSpace>.Of(space), row);
  }
}
