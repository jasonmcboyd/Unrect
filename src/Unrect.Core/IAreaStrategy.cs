namespace Unrect.Core
{
  /// <summary>
  /// The area a region occupies — a size strategy under the name a placement declares it by.
  /// The same machine, <see cref="ISizeScan"/>; what a whole region answers is
  /// <see cref="Scans.GetArea"/>.
  /// </summary>
  public interface IAreaStrategy
  {
    /// <summary>A fresh scan, to be shown spans along <paramref name="along"/>.</summary>
    ISizeScan Begin(Orientation along);
  }
}
