using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// What a projection produced and how much of its extent it used.
  /// </summary>
  public readonly struct ShapeResult<T>
  {
    public ShapeResult(T value, Size consumed)
    {
      Value = value;
      Consumed = consumed;
    }

    public T Value { get; }
    public Size Consumed { get; }
  }

  /// <summary>
  /// A shape applied to a space: the projected value, the offset its placement resolved to, and the
  /// extent it consumed. <see cref="Advance"/> is what a caller must step to get past it.
  /// </summary>
  public readonly struct AppliedResult<T>
  {
    public AppliedResult(T value, Offset offset, Size consumed)
    {
      Value = value;
      Offset = offset;
      Consumed = consumed;
    }

    public T Value { get; }
    public Offset Offset { get; }
    public Size Consumed { get; }
    public Size Advance => Offset.Size + Consumed;
  }
}
