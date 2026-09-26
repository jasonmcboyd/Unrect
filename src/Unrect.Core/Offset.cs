using System;

namespace Unrect.Core
{
  /// <summary>
  /// Where a region starts within the one it was cut from: a displacement of so many columns across
  /// and so many rows down, both non-negative. A displacement and an extent are different things —
  /// one is a position, the other a size — so an offset is not a <see cref="Size"/> and does not
  /// convert to one; the one thing they do together is compose, an origin and the extent reached
  /// from it.
  /// </summary>
  public readonly struct Offset : IEquatable<Offset>
  {
    /// <summary>A displacement of <paramref name="column"/> columns across and <paramref name="row"/> rows down; either negative throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public Offset(int column, int row)
    {
      if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));
      if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));

      Column = column;
      Row = row;
    }

    /// <summary>How many columns across.</summary>
    public int Column { get; }

    /// <summary>How many rows down.</summary>
    public int Row { get; }

    /// <summary>Composes two displacements — a slice's origin onto its parent's.</summary>
    public static Offset operator +(Offset first, Offset second)
      => new Offset(first.Column + second.Column, first.Row + second.Row);

    /// <summary>
    /// The extent reached from <paramref name="origin"/> by <paramref name="extent"/>: how far a
    /// region that starts at the origin and is that big reaches from the corner it was measured from.
    /// </summary>
    public static Size operator +(Offset origin, Size extent)
      => new Size(origin.Column + extent.Width, origin.Row + extent.Height);

    /// <summary>Whether <paramref name="other"/> is the same displacement.</summary>
    public bool Equals(Offset other) => Column == other.Column && Row == other.Row;

    /// <summary>Equality against any object — see <see cref="Equals(Offset)"/> when the other value is an offset.</summary>
    public override bool Equals(object? obj) => obj is Offset other && Equals(other);

    /// <summary>Consistent with <see cref="Equals(Offset)"/>.</summary>
    public override int GetHashCode() => Hashes.Combine(Column, Row);

    /// <summary>Same as <see cref="Equals(Offset)"/>.</summary>
    public static bool operator ==(Offset first, Offset second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Offset first, Offset second) => !(first == second);

    /// <summary>The displacement as <c>(column,row)</c> — the same rendering <see cref="Plane{TSpace}"/> gives its origin.</summary>
    public override string ToString() => $"({Column},{Row})";
  }
}
