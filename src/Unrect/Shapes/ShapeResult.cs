using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// What a projection produced and how much of its extent it used.
  /// </summary>
  public readonly struct ShapeResult<T>
  {
    /// <summary>Creates a result carrying <paramref name="value"/> and how much of the extent it used.</summary>
    public ShapeResult(T value, Size consumed)
    {
      Value = value;
      Consumed = consumed;
    }

    /// <summary>The projected value.</summary>
    public T Value { get; }

    /// <summary>How much of the extent handed to <c>Project</c> the projection used.</summary>
    public Size Consumed { get; }
  }

  /// <summary>
  /// A shape applied to a space: the projected value, the offset its placement resolved to, and the
  /// extent it consumed. <see cref="Advance"/> is what a caller must step to get past it.
  /// </summary>
  public readonly struct AppliedResult<T>
  {
    /// <summary>Creates a result carrying where the shape landed and what it produced.</summary>
    public AppliedResult(T value, Offset offset, Size consumed)
    {
      Value = value;
      Offset = offset;
      Consumed = consumed;
    }

    /// <summary>The projected value.</summary>
    public T Value { get; }

    /// <summary>Where the shape's placement resolved to, relative to the space it was applied to.</summary>
    public Offset Offset { get; }

    /// <summary>How much of its own extent, measured from <see cref="Offset"/>, the shape used.</summary>
    public Size Consumed { get; }

    /// <summary>What a caller must step past this shape: <see cref="Offset"/> plus <see cref="Consumed"/>.</summary>
    public Size Advance => Offset.Size + Consumed;
  }
}
