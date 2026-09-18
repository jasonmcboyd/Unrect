namespace Unrect.Projections
{
  /// <summary>
  /// Which way a projection runs, and which way a source hands the engine its spans. The
  /// vocabulary spells the axis into a factory's name (<c>VerticalFlow</c>, <c>Row</c>,
  /// <c>UntilColumn</c>) rather than taking it as an argument; it appears in a signature only where
  /// the driver is the subject — the <see cref="CostReport"/> of a declaration under a row or a
  /// column driver.
  /// </summary>
  public enum Orientation
  {
    /// <summary>Along a row: a horizontal flow, a column span.</summary>
    Horizontal,

    /// <summary>Down a column: a vertical flow, a row span — how a row-major source is driven.</summary>
    Vertical
  }
}
