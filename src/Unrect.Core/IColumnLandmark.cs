namespace Unrect.Core
{
  /// <summary>
  /// The first column that is some piece of content — the column twin of <see cref="IRowLandmark"/>:
  /// it only reports what it found, so a caller may decide for itself what an absent one means.
  /// </summary>
  public interface IColumnLandmark
  {
    /// <summary>What is being looked for, phrased as a negative noun: <c>no column saying 'Total'</c>.</summary>
    string Description { get; }

    /// <summary>The index of the first column of <paramref name="region"/> that is the landmark, or null when there is none.</summary>
    int? FindColumn(Plane<ISpace> region);
  }
}
