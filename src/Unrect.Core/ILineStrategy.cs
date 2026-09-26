namespace Unrect.Core
{
  /// <summary>
  /// Which leading lines of a region belong to it — rows when <see cref="Along"/> is
  /// <see cref="Orientation.Vertical"/>, columns when it is <see cref="Orientation.Horizontal"/> —
  /// declared as the machine that decides, one line at a time. What a whole region answers is the
  /// fold of the scan: <see cref="Scans.SelectLines{TSpace}"/>. A rule is about one axis and says which, so
  /// a member that wants rows can refuse a rule about columns where the declaration is written.
  /// </summary>
  /// <typeparam name="TSpace">The space this rule reads.</typeparam>
  public interface ILineStrategy<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The axis this rule counts along: rows down a column, or columns along a row.</summary>
    Orientation Along { get; }

    /// <summary>A fresh scan, to be asked line by line.</summary>
    ILineScan<TSpace> Begin();
  }
}
