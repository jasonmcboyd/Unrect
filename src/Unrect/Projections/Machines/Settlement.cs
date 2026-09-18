using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a machine yields at <c>Close</c>: the value, how much of its extent it kept — both
  /// dimensions, because a parent across the machine's axis needs the other one — and the presence
  /// it reports, as a projection result does today.
  /// </summary>
  /// <typeparam name="TResult">What the machine reads.</typeparam>
  public readonly struct Settlement<TResult>
  {
    /// <summary>A settlement of <paramref name="value"/> over <paramref name="consumed"/>, reporting <paramref name="presence"/>.</summary>
    public Settlement(TResult value, Size consumed, Presence presence)
    {
      Value = value;
      Consumed = consumed;
      Presence = presence;
    }

    /// <summary>What the machine read.</summary>
    public TResult Value { get; }

    /// <summary>The extent the machine kept, width and height, from the origin it was placed at.</summary>
    public Size Consumed { get; }

    /// <summary>Whether the machine read content, read an empty region, or absorbed a failure without looking.</summary>
    public Presence Presence { get; }
  }
}
