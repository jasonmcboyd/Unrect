namespace Unrect.Core
{
  /// <summary>
  /// The first line that is some piece of content — a caption, a total line, a rule — a row when
  /// <see cref="Along"/> is <see cref="Orientation.Vertical"/>, a column when it is
  /// <see cref="Orientation.Horizontal"/>. Where a lifted strategy lifts a match into an offset and
  /// throws when there is none, a landmark only reports what it found, so a caller may decide for
  /// itself what an absent one means.
  /// </summary>
  /// <typeparam name="TSpace">The space this landmark reads.</typeparam>
  public interface ILineLandmark<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The axis the index it finds counts along: rows down a column, or columns along a row.</summary>
    Orientation Along { get; }

    /// <summary>
    /// What is being looked for, phrased as a negative noun so a failure reads as a sentence:
    /// <c>no row saying 'Total'</c>.
    /// </summary>
    string Description { get; }

    /// <summary>The index of the first line of <paramref name="region"/> that is the landmark, or null when there is none.</summary>
    int? Find(Plane<TSpace> region);
  }
}
