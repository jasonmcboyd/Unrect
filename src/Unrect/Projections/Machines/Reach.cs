using System;

namespace Unrect.Projections
{
  /// <summary>
  /// How far back the engine may need to reach for a machine before it settles: none, a fixed
  /// number of spans, or as far as the machine's own extent runs. A definition's promise to the
  /// parent that starts it: <see cref="None"/> means what the machine accepts it keeps, so no hold
  /// need be opened for it. Joins upward by <see cref="Max"/>.
  /// </summary>
  public readonly struct Reach : IEquatable<Reach>
  {
    private readonly int _spans;   // 0: none; positive: that many spans; -1: the extent

    private Reach(int spans) => _spans = spans;

    /// <summary>What I accept, I keep.</summary>
    public static Reach None => new Reach(0);

    /// <summary>At most <paramref name="count"/> spans may be handed back — a pad's bottom, a tiler's partial band.</summary>
    public static Reach Spans(int count)
      => count >= 0 ? new Reach(count) : throw new ArgumentOutOfRangeException(nameof(count), count, "A reach cannot be negative.");

    /// <summary>As far as the machine's extent runs, however long that turns out to be.</summary>
    public static Reach Extent => new Reach(-1);

    /// <summary>True for <see cref="None"/>.</summary>
    public bool IsNone => _spans == 0;

    /// <summary>True for <see cref="Extent"/>.</summary>
    public bool IsExtent => _spans < 0;

    /// <summary>The bounded count, or null for <see cref="Extent"/>.</summary>
    public int? Count => _spans < 0 ? (int?)null : _spans;

    /// <summary>The further of the two: <see cref="Extent"/> if either is, else the larger count.</summary>
    public static Reach Max(Reach first, Reach second)
      => first.IsExtent || second.IsExtent ? Extent : new Reach(Math.Max(first._spans, second._spans));

    /// <inheritdoc cref="Max"/>
    public Reach Join(Reach other) => Max(this, other);

    /// <inheritdoc/>
    public bool Equals(Reach other) => _spans == other._spans;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Reach other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _spans;

    /// <summary>Value equality.</summary>
    public static bool operator ==(Reach first, Reach second) => first.Equals(second);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(Reach first, Reach second) => !first.Equals(second);

    /// <inheritdoc/>
    public override string ToString() => IsNone ? "none" : IsExtent ? "extent" : _spans == 1 ? "1 span" : $"{_spans} spans";
  }
}
