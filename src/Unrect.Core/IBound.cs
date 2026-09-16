namespace Unrect.Core
{
  /// <summary>
  /// A region's bottom edge, discovered while the region is read rather than measured before the
  /// reading starts — what a <see cref="Plane{TSpace}"/> carries when its height is not yet settled.
  /// <para>
  /// The declaration still decides the boundary: the rule is written before any data is seen. Only
  /// the moment it runs has moved, which is what lets a shape stream — a walk asks
  /// <see cref="HasRow"/> one row at a time and reads no further than it has to, where asking how
  /// tall the region is reads to exhaustion.
  /// </para>
  /// <para>
  /// Rows are counted from this bound's own first row, and the width is the plane's business rather
  /// than the bound's. <see cref="Shift"/> is how a region that starts part-way down gets a bound
  /// that agrees with it, so a caller never adds an origin to a row number itself.
  /// </para>
  /// </summary>
  public interface IBound
  {
    /// <summary>
    /// Whether there is a row at <paramref name="row"/>, reading only as far as it takes to say.
    /// A false answer means the discovery stopped at or before that row, so the height is settled
    /// by the time anything needs to say what it was.
    /// </summary>
    bool HasRow(int row);

    /// <summary>
    /// The settled height, which means reading to exhaustion. Idempotent, and free once anything
    /// has settled the bound.
    /// </summary>
    int Force();

    /// <summary>
    /// The same discovery counted from <paramref name="rows"/> rows further down — what a region
    /// offset into a bounded one carries, so that its row 0 is the bound's row
    /// <paramref name="rows"/>. It shares the discovery rather than restarting it, so a region
    /// stepped through in pieces still reads each row exactly once.
    /// </summary>
    IBound Shift(int rows);
  }
}
