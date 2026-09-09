using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a projection produced and how much of its extent it used.
  /// </summary>
  public readonly struct ProjectionResult<T>
  {
    /// <summary>Creates a result carrying <paramref name="value"/> and how much of the extent it used.</summary>
    public ProjectionResult(T value, Size consumed)
      : this(value, consumed, Presence.Read)
    {
    }

    /// <summary>
    /// The same result, saying what kind of something or nothing it was. A second constructor
    /// rather than a parameter on the public one, and not by preference: <see
    /// cref="Projections.Presence"/> is internal, so it cannot appear in a public signature at all.
    /// The public constructor therefore reports <see cref="Projections.Presence.Read"/>, which is
    /// the right answer for every projection written outside this library and is what the enum's
    /// zero-valued default already says.
    /// </summary>
    internal ProjectionResult(T value, Size consumed, Presence presence)
    {
      Value = value;
      Consumed = consumed;
      Presence = presence;
    }

    /// <summary>The projected value.</summary>
    public T Value { get; }

    /// <summary>How much of the extent handed to <c>Project</c> the projection used.</summary>
    public Size Consumed { get; }

    /// <inheritdoc cref="Projections.Presence"/>
    internal Presence Presence { get; }
  }

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

    /// <inheritdoc cref="ProjectionResult{T}(T, Size, Presence)"/>
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
