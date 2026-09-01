namespace Unrect.Core
{
  /// <summary>The column twin of <see cref="IRowLandmark"/>.</summary>
  public interface IColumnLandmark
  {
    /// <summary>What is being looked for, e.g. <c>no column containing 'Total'</c>.</summary>
    string Description { get; }

    /// <summary>The index of the first column that is the landmark, or null when there is none.</summary>
    int? FindColumn(ISpace space);
  }
}
