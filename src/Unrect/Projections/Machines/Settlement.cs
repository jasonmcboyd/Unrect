using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a machine yields at <c>Close</c>: the value, how much of its extent it kept — both
  /// dimensions, because a parent across the machine's axis needs the other one — and whether it
  /// absorbed a failure rather than reading.
  /// </summary>
  /// <typeparam name="TResult">What the machine reads.</typeparam>
  public readonly struct Settlement<TResult>
  {
    /// <summary>A settlement that read what it consumed.</summary>
    public Settlement(TResult value, Size consumed)
      : this(value, consumed, absorbed: false)
    {
    }

    /// <summary>A settlement of <paramref name="value"/> over <paramref name="consumed"/>, saying whether a tolerance boundary absorbed a failure to reach it.</summary>
    public Settlement(TResult value, Size consumed, bool absorbed)
    {
      Value = value;
      Consumed = consumed;
      Absorbed = absorbed;
    }

    /// <summary>What the machine read.</summary>
    public TResult Value { get; }

    /// <summary>The extent the machine kept, width and height, from the origin it was placed at.</summary>
    public Size Consumed { get; }

    /// <summary>
    /// True when a tolerance boundary absorbed a failure and supplied its filler without reading:
    /// the extent is then honestly unknown, which a repeat reads to tell an ended run from an empty
    /// one. False for everything that looked, whether it found content or nothing.
    /// </summary>
    public bool Absorbed { get; }
  }
}
