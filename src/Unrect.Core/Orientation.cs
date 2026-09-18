namespace Unrect.Core
{
  /// <summary>
  /// Which way something runs: the axis a strategy is asked along, the spans a source hands the
  /// engine, the direction a flow lays its children out. A row-major source is driven
  /// <see cref="Vertical"/>: one row span after another, down the sheet.
  /// </summary>
  public enum Orientation
  {
    /// <summary>Along a row: column spans, a horizontal flow.</summary>
    Horizontal,

    /// <summary>Down a column: row spans, a vertical flow — how a row-major source is driven.</summary>
    Vertical,
  }
}
