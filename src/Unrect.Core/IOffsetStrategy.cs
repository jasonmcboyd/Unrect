namespace Unrect.Core
{
  /// <summary>How a shape's origin is found within the space it is handed — a fixed skip, a seek past blanks, a landmark.</summary>
  public interface IOffsetStrategy
  {
    /// <summary>Where to start, relative to the top-left of <paramref name="availableSpace"/>. Throws <see cref="OutOfBoundsException"/> when nothing satisfies it.</summary>
    Offset GetOffset(ISpace availableSpace);
  }
}
