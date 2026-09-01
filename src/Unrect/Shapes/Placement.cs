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
    // The one shared "no offset declared yet" strategy, so HasDeclaredOffset is a reference test.
    // Declared first because the static initializers below run in textual order.
    private static readonly IOffsetStrategy NoOffset = OffsetStrategies.MinOffset();

    public Placement(IOffsetStrategy offset, IAreaStrategy? area)
    {
      Offset = offset ?? throw new ArgumentNullException(nameof(offset));
      Area = area;
    }

    public static Placement Default { get; } = new Placement(NoOffset, null);

    public static Placement Of(IAreaStrategy area) => new Placement(NoOffset, NotNull(area));

    public IOffsetStrategy Offset { get; }
    public IAreaStrategy? Area { get; }

    public Placement WithOffset(IOffsetStrategy offset) => new Placement(offset, Area);

    public Placement WithArea(IAreaStrategy area) => new Placement(Offset, NotNull(area));

    /// <summary>
    /// False while the shape simply sits where it is handed. Offset modifiers compose onto an
    /// offset the shape already declared; until one exists there is nothing to compose with, and
    /// composing with a no-op would only blur the diagnostics when the new offset does not fit.
    /// </summary>
    internal bool HasDeclaredOffset => !ReferenceEquals(Offset, NoOffset);

    // Only the constructor takes a null area, where it deliberately means "derive the extent".
    // Everywhere else a null would silently turn a declared extent into a derived one.
    private static IAreaStrategy NotNull(IAreaStrategy area) => area ?? throw new ArgumentNullException(nameof(area));
  }
}
