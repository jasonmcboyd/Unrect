using Unrect.Core;

namespace Unrect
{
  /// <summary>
  /// A space that wants to be told which band a declaration is about to read. Optional in every
  /// sense: nothing in the contracts mentions it, the engine finds it by a type test, and a space
  /// that does not implement it is read exactly as it always was.
  /// <para>
  /// It exists for a backend that holds only part of its data in memory. Cutting a region is
  /// arithmetic, so nothing about the objects handed around says which rows are live; this is how
  /// that is said instead — once per placement rather than once per cell, and never as an
  /// instruction. A space may do what it likes with the announcement, including nothing.
  /// </para>
  /// </summary>
  public interface ISweepAware
  {
    /// <summary>
    /// The region a projection has just been placed in: where it starts in this space's own
    /// coordinates, and how big it was <em>declared</em> to be.
    /// </summary>
    /// <param name="origin">Where the region starts, in this space's own coordinates.</param>
    /// <param name="area">
    /// How big the region was declared. A region whose bottom edge is still being discovered
    /// announces the ceiling it sits under, because asking where it really ends would read the file
    /// to find out — which is the cost the announcement exists to avoid.
    /// </param>
    void Sweeping(Offset origin, Area area);
  }
}
