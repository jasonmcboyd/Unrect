using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A projection applied to a space: the projected value, the offset its placement resolved to,
  /// and the extent it consumed. <see cref="Advance"/> is what a caller must step to get past it.
  /// </summary>
  public readonly struct AppliedResult<T>
  {
    /// <summary>Creates a result carrying where the projection landed and what it produced.</summary>
    public AppliedResult(T value, Offset offset, Size consumed)
      : this(value, offset, consumed, Presence.Read)
    {
    }

    /// <summary>The same, with what the projection's presence says about a zero extent.</summary>
    internal AppliedResult(T value, Offset offset, Size consumed, Presence presence)
    {
      Value = value;
      Offset = offset;
      Consumed = consumed;
      Presence = presence;
    }

    /// <summary>The projected value.</summary>
    public T Value { get; }

    /// <summary>Where the projection's placement resolved to, relative to the space it was applied to.</summary>
    public Offset Offset { get; }

    /// <summary>How much of its own extent, measured from <see cref="Offset"/>, the projection used.</summary>
    public Size Consumed { get; }

    /// <summary>What a caller must step past this projection: <see cref="Offset"/> plus <see cref="Consumed"/>.</summary>
    public Size Advance => Offset.Size + Consumed;

    /// <inheritdoc cref="Projections.Presence"/>
    internal Presence Presence { get; }
  }
}
