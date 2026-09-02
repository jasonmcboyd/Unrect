namespace Unrect.Core
{
  /// <summary>How a shape's declared extent is found, once its origin is already known.</summary>
  public interface IAreaStrategy
  {
    /// <summary>The rectangle to use, measured from the top-left of <paramref name="availableSpace"/>. Throws <see cref="OutOfBoundsException"/> when none fits.</summary>
    Area GetArea(ISpace availableSpace);
  }
}
