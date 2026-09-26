using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// "This whole space, as a region" — the one translation the strategy suites needed when the
  /// calculus started taking regions instead of spaces.
  /// <para>
  /// Every fixture here hands back a space, and every one of these tests means the whole of it. That
  /// is exactly <c>Plane&lt;ISpace&gt;.Of(space)</c>, so these overloads say it once rather than at
  /// six hundred call sites. They add no behaviour: a region over a whole space with origin (0, 0)
  /// is the space, and a test that wants a region of part of one still writes the slice out.
  /// </para>
  /// </summary>
  internal static class WholeSpace
  {
    public static Plane<ISpace> Region(this ICellSpace space) => Plane<ISpace>.Of(space);

    public static Size GetSize(this ISizeStrategy strategy, ICellSpace space) => strategy.GetSize(space.Region());

    public static Offset GetOffset(this IOffsetStrategy strategy, ICellSpace space) => strategy.GetOffset(space.Region());

    public static int SelectLines(this ILineStrategy strategy, ICellSpace space) => strategy.SelectLines(space.Region());

    public static int? Find(this ILineLandmark landmark, ICellSpace space) => landmark.Find(space.Region());

    public static bool Includes(this ILineScan scan, ICellSpace space, int row) => scan.Includes(space.Region(), row);
  }
}
