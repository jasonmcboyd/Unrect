namespace Unrect.Core
{
  /// <summary>How a shape's extent is discovered from the space it is handed, rather than declared as a fixed number.</summary>
  public interface ISizeStrategy
  {
    /// <summary>The extent to use, measured from the top-left of <paramref name="availableSpace"/>. Throws <see cref="OutOfBoundsException"/> when none fits.</summary>
    Size GetSize(ISpace availableSpace);
  }
}
