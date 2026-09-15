using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A region whose bottom edge is still being discovered, wearing a space's clothes — the one place
  /// a plane is turned back into an <see cref="ICellValues"/>, because the strategy layer still
  /// takes spaces rather than regions.
  /// <para>
  /// A strategy handed one of these forces it, and always has: strategies read
  /// <see cref="ISpace.Area"/> freely, so asking a still-being-discovered region how tall it is
  /// reads the discovery to exhaustion exactly where it did before. What matters is that it forces
  /// at the same moment and reads through the same space — a slice cut to the settled height would
  /// tell the streaming store a different band was open, moving the eviction counters without moving
  /// a single answer.
  /// </para>
  /// <para>
  /// <b>Every use of it is a strategy call.</b> It exists because strategies take spaces; a strategy
  /// that took a region would need none of it, and neither would this type.
  /// </para>
  /// </summary>
  internal sealed class BoundedView : ICellValues, ISpaceChart
  {
    private readonly Plane<ICellValues> _extent;

    internal BoundedView(Plane<ICellValues> extent) => _extent = extent;

    /// <summary>
    /// The chart's underlying space. A bound narrows a height and moves nothing, so a capability
    /// found through here answers about exactly the cells this space addresses — the condition
    /// <see cref="ISpaceChart"/> imposes, met here by construction. Without it a matcher that
    /// demands a capability would be told the file has none, one bounded extent down.
    /// </summary>
    ICellValues ISpaceChart.Underlying => _extent.Space;

    /// <summary>
    /// The extent, which means reading the discovery to exhaustion. An area is a
    /// pair of numbers and there is no answering half of one, which is why the engine asks the
    /// region for its width and its rows instead.
    /// </summary>
    public Area Area => _extent.Area;

    /// <inheritdoc/>
    public CellValue this[int column, int row] => _extent.CellAt(column, row);

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => this[column, row].IsBlank;

    /// <inheritdoc/>
    public bool IsText(int column, int row) => this[column, row].IsText;

    /// <inheritdoc/>
    public string? AsText(int column, int row) => this[column, row].AsText();

    /// <summary>
    /// An ordinary subspace, as slicing a still-being-discovered region has always given: the
    /// rectangle was just named, so its extent is no longer anybody's to discover. The rows asked
    /// for are admitted one at a time, so slicing reads no further than the slice reaches.
    /// </summary>
    public ICellValues GetSubspace(Offset offset, Area area) => _extent.Cut(offset, area).Space;
  }
}
