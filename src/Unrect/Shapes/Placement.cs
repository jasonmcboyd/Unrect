using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// Where a shape sits inside the space it is handed: an offset to its origin and, optionally, an
  /// area. A null <see cref="Area"/> means the extent is derived from the shape's own content or
  /// children rather than declared.
  /// </summary>
  public sealed class Placement
  {
    public Placement(IOffsetStrategy offset, IAreaStrategy? area)
    {
      Offset = offset ?? throw new ArgumentNullException(nameof(offset));
      Area = area;
    }

    public static Placement Default { get; } = new Placement(OffsetStrategies.MinOffset(), null);

    public static Placement Of(IAreaStrategy area) => new Placement(OffsetStrategies.MinOffset(), NotNull(area));

    public IOffsetStrategy Offset { get; }
    public IAreaStrategy? Area { get; }

    public Placement WithOffset(IOffsetStrategy offset) => new Placement(offset, Area);

    public Placement WithArea(IAreaStrategy area) => new Placement(Offset, NotNull(area));

    // Only the constructor takes a null area, where it deliberately means "derive the extent".
    // Everywhere else a null would silently turn a declared extent into a derived one.
    private static IAreaStrategy NotNull(IAreaStrategy area) => area ?? throw new ArgumentNullException(nameof(area));
  }
}
