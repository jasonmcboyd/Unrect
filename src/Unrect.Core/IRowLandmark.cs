namespace Unrect.Core
{
  /// <summary>
  /// The first row that is some piece of content — a caption, a total line, a rule. Where a seek
  /// strategy lifts a match into an offset and throws when there is none, a landmark only reports
  /// what it found, so a caller may decide for itself what an absent one means.
  /// </summary>
  public interface IRowLandmark
  {
    /// <summary>
    /// What is being looked for, phrased as the seeks phrase it so the two read alike:
    /// <c>no row containing 'Total'</c>.
    /// </summary>
    string Description { get; }

    /// <summary>The index of the first row that is the landmark, or null when there is none.</summary>
    int? FindRow(ISpace space);
  }
}
